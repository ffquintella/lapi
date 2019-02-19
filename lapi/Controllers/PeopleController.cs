using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static lapi.domain.LoggingEvents;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using lapi.Ldap;
using System.Text.RegularExpressions;
using lapi.domain;
using lapi.Web;

namespace lapi.Controllers
{
    //[Produces("application/json")]
    [Authorize(Policy = "Reading")]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [ApiController]
    public class PeopleController : BaseController
    {


        public PeopleController(ILogger<PeopleController> logger, IConfiguration iConfig)
        {

            base.logger = logger;

            configuration = iConfig;
        }

        #region GET
        
        // GET api/people
        [HttpGet]
        public ActionResult<IEnumerable<String>> Get([FromQuery]int _start, [FromQuery]int _end)
        {

            this.ProcessRequest();

            logger.LogInformation(ListItems, "{0} listing all users", requesterID);


            var uManager = PeopleManager.Instance;

            if (_start == 0 && _end != 0)
            {
                return Conflict();
            }

            if (_start == 0 && _end == 0) return uManager.GetList();
            else
            {
                throw new NotImplementedException();
                //return uManager.GetList(_start, _end);
            }

            //return users;

        }


        // GET api/people 
        [HttpGet]
        public ActionResult<IEnumerable<domain.Person>> Get([RequiredFromQuery]bool _full, [FromQuery]int _start, [FromQuery]int _end)
        {
            if (_full)
            {
                this.ProcessRequest();

                logger.LogInformation(ListItems, "{0} getting all users objects", requesterID);

                if (_start == 0 && _end != 0)
                {
                    return Conflict();
                }

                var uManager = PeopleManager.Instance;
                List<domain.Person> users;

                if (_start == 0 && _end == 0) users = uManager.GetPeople();
                else
                {
                    throw new NotImplementedException();
                    //users = uManager.GetUsers(_start, _end);
                }

                return users;
            }
            else
            {
                return new List<domain.Person>();
            }
        }

        // GET api/people/search/:filter
        [HttpGet("search/{filter}")]
        public ActionResult<IEnumerable<String>> SearchGet(string filter)
        {

            this.ProcessRequest();

            logger.LogInformation(ListItems, "{0} searching users", requesterID);


            var pManager = PeopleManager.Instance;

            return pManager.GetList(filter);
   

        }
        
        // GET api/people/ingroup/:group
        [HttpGet("ingroup/{group}")]
        public ActionResult<IEnumerable<domain.Person>> GetPeopleInGroup(string group)
        {

            this.ProcessRequest();

            logger.LogInformation(ListItems, "{0} searching users in group {1}", requesterID, group);


            var pManager = PeopleManager.Instance;

            return pManager.GetPeopleInGroup(group);


        }

        // GET api/people/:person
        [HttpGet("{DN}")]
        public ActionResult<domain.Person> Get(string DN)
        {
            this.ProcessRequest();
            var uManager = PeopleManager.Instance;

            var user = uManager.GetPerson(DN);
            logger.LogDebug(GetItem, "User DN={dn} found", DN);

            return user;
        }


        //[ProducesResponseType(200, Type = typeof(Product))]
        //[ProducesResponseType(404)]

        // GET api/users/:user/exists
        [HttpGet("{DN}/exists")]
        public IActionResult GetExists(string DN)
        {
            this.ProcessRequest();

            var uManager = PeopleManager.Instance;

            try
            {
                logger.LogDebug(ItemExists, "User DN={dn} found");
                var user = uManager.GetPerson(DN);

                if (user == null) return NotFound();
                    
                return Ok();

            }
            catch (Exception)
            {
                logger.LogDebug(ItemExists, "User DN={dn} not found.");
                return NotFound();
            }

        }

        // GET api/people/:person/member-of
        [HttpGet("{DN}/member-of")]
        public ActionResult<List<string>> MemberOf(string DN)
        {
            this.ProcessRequest();

            var pManager = PeopleManager.Instance;

            try
            {

                var groups = pManager.GetPersonGroups(DN);

                if (groups == null) return NotFound();

                return groups;

            }
            catch (Exception)
            {
                logger.LogDebug(ItemExists, "Error listing groups of DN={dn}.");
                return NotFound();
            }
            
            throw new NotImplementedException();
            

        }
        #endregion

        #region Authentication

        // GET api/users/:user/authenticate
        [HttpPost("{DN}/authenticate")]
        public ActionResult Authenticate(string DN, [FromBody] AuthenticationRequest req)
        {

            var uManager = PeopleManager.Instance;
            var duser = uManager.GetPerson(DN);

            if (duser == null)
            {
                logger.LogDebug(PutItem, "User DN={dn} found", DN);
                return NotFound();
            }
            else
            {


                var success = uManager.ValidateAuthentication(DN, req.Password);

                if (success) return Ok();
                return StatusCode(401);
            }

        }

        // GET api/users/authenticate
        [HttpPost("authenticate")]
        public ActionResult AuthenticateDirect([FromBody] AuthenticationRequest req)
        {

            var uManager = PeopleManager.Instance;

            string login;

            if (req.Login == null)
            {
                logger.LogDebug(AuthenticationItem, "Invalid Authentication request without login");
                return BadRequest();
            }
            else login = req.Login;

            var success = uManager.ValidateAuthentication(login, req.Password);

            if (success) return Ok();
            return StatusCode(401);


        }
        #endregion

        #region PUT
        // PUT api/users/:user
        /// <summary>
        /// Creates the specified user.
        /// </summary>
        /// <returns>The put.</returns>
        /// <param name="person">User.</param>
        [Authorize(Policy = "Writting")]
        [HttpPut("{DN}")]
        public ActionResult Put(string DN, [FromBody] Person person)
        {
            ProcessRequest();

            logger.LogDebug(PutItem, "Tring to create user:{0}", DN);

            if (ModelState.IsValid)
            {
                if (person.DN != null && person.DN != DN)
                {
                    logger.LogError(PutItem, "User DN different of the URL DN in put request user.DN={0} DN={1}", person.DN, DN);
                    return Conflict();
                }


                //Regex regex = new Regex(@"cn=([^,]+?),", RegexOptions.IgnoreCase);
                Regex regex = new Regex(@"\Acn=(?<login>[^,]+?),", RegexOptions.IgnoreCase);

                Match match = regex.Match(DN);

                if (!match.Success)
                {
                    logger.LogError(PutItem, "DN is not correcly formated  DN={0}", DN);
                    return Conflict();
                }

                var uLogin = match.Groups["login"];

                var uManager = PeopleManager.Instance;

                var aduser = uManager.GetPerson(DN);

                person.DN = DN;

                if (person.Surname == null) person.Surname = "---";
                
                if (aduser == null)
                {
                    // New User
                    logger.LogInformation(InsertItem, "Creating user DN={DN}", DN);


                    var result = uManager.CreatePerson(person);
                    if (result == 0) return Ok();
                    else return this.StatusCode(500);

                }
                else
                {
                    // Update 
                    logger.LogInformation(UpdateItem, "Updating user DN={DN}", DN);


                    var result = uManager.SavePerson(person);
                    if (result == 0) return Ok();
                    else return this.StatusCode(500);

                }



            }
            else
            {
                return BadRequest();
            }

        }

        #endregion
        
        #region DELETE
        /// <summary>
        /// Delete the specified DN.
        /// </summary>
        /// <response code="200">Deleted Ok</response>
        /// <response code="404">OU not found</response>
        /// <response code="500">Internal Server error</response>
        [Authorize(Policy = "Writting")]
        [HttpDelete("{DN}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(500)]
        public ActionResult Delete(string DN)
        {
            ProcessRequest();

            logger.LogDebug(PutItem, "Tring to delete User:{0}", DN);

            Regex regex = new Regex(@"\Acn=(?<login>[^,]+?),", RegexOptions.IgnoreCase);

            Match match = regex.Match(DN);

            if (!match.Success)
            {
                logger.LogError(PutItem, "DN is not correcly formated  DN={0}", DN);
                return Conflict();
            }



            var uManager = PeopleManager.Instance;

            var duser = uManager.GetPerson(DN);

            if (duser == null)
            {
                // No User
                logger.LogError(DeleteItem, "Tring to delete unexistent User DN={DN}", DN);

                return NotFound();

            }
            else
            {
                // Delete 
                logger.LogInformation(DeleteItem, "Deleting user DN={DN}", DN);

                var result = uManager.DeleteUser(duser);
                if (result == 0) return Ok();
                else return this.StatusCode(500);

            }


        }
        #endregion

    }

}
