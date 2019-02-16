using System;
using System.Collections.Generic;
using System.Linq;
using Novell.Directory.Ldap;
using System.Text;
using NLog;
using lapi.domain;
using lapi.Ldap;
using lapi.Tools;

namespace lapi
{
    public class PeopleManager: ObjectManager
    {

        #region SINGLETON

        private static readonly Lazy<PeopleManager> lazy = new Lazy<PeopleManager>(() => new PeopleManager());

        public static PeopleManager Instance { get { return lazy.Value; } }

        private PeopleManager()
        {
            logger = NLog.LogManager.GetCurrentClassLogger();
        }

        #endregion

        /// <summary>
        /// Return a string list of the users DNs
        /// </summary>
        /// <returns>The list.</returns>
        public List<String> GetList()
        {
            var users = new List<String>();

            var sMgmt = LdapQueryManager.Instance;

            int results = 0;

            var resps = sMgmt.ExecuteSearch("", LdapSearchType.User);
            
            //var resps = sMgmt.ExecutePagedSearch("", LdapSearchType.User);

            foreach(var entry in resps)
            {
                users.Add(entry.Dn);
                results++;
            }

            logger.Debug("User search executed results:{result}", results);


            return users;
        }

        //TODO: Verify the lower limit witch is not working

        /// <summary>
        /// Gets the list. Limited to a start and end number based on the total colection sorted by the name
        /// </summary>
        /// <returns>The list.</returns>
        /// <param name="start">Start.</param>
        /// <param name="end">End.</param>
        public List<String> GetList(int start, int end)
        {
            var users = new List<String>();

            var sMgmt = LdapQueryManager.Instance;

            int results = 0;


            var resps = sMgmt.ExecuteLimitedSearch("", LdapSearchType.User, start, end);

            foreach (var entry in resps)
            {
                users.Add(entry.GetAttribute("distinguishedName").StringValue);
                results++;
            }

            logger.Debug("User search executed results:{result}", results);


            return users;
        }

        /// <summary>
        /// Gets the list of all users.
        /// </summary>
        /// <returns>The users.</returns>
        public List<Person> GetPeople()
        {

            var users = new List<Person>();

            var sMgmt = LdapQueryManager.Instance;

            var resps = sMgmt.ExecuteSearch("", LdapSearchType.User);
            int results = 0;

            foreach (var entry in resps)
            {
                users.Add(ConvertfromLdap(entry));
                results++;
            }

            logger.Debug("People search executed results:{result}", results);


            return users;
        }


        public List<Person> GetPeople(int start, int end)
        {
            var users = new List<Person>();

            var sMgmt = LdapQueryManager.Instance;

            int results = 0;


            var resps = sMgmt.ExecuteLimitedSearch("", LdapSearchType.User, start, end);

            foreach (var entry in resps)
            {
                users.Add(ConvertfromLdap(entry));
                results++;
            }

            logger.Debug("People search executed results:{result}", results);


            return users;
        }

        /// <summary>
        /// Gets the user.
        /// </summary>
        /// <returns>The user.</returns>
        /// <param name="DN">The Disitnguesh name of the user</param>
        public Person GetPerson (string DN)
        {
            var sMgmt = LdapQueryManager.Instance;

            try
            {
                var entry = sMgmt.GetRegister(DN);
                var user = ConvertfromLdap(entry);
                return user;
            }catch(LdapException ex)
            {
                logger.Debug("Person not found {0} Ex: {1}", DN, ex.Message);
                return null;
            }

        }

        /// <summary>
        /// Creates the user on LDAP Directory.
        /// </summary>
        /// <returns> -1 Error </returns>
        /// <returns> 0 OK </returns>
        /// <param name="person">User.</param>
        public int CreatePerson(Person person)
        {

            //Creates the List attributes of the entry and add them to attributeset

            LdapAttributeSet attributeSet = GetAttributeSet(person);

            // DN of the entry to be added
            string dn = person.DN;

            LdapEntry newEntry = new LdapEntry(dn, attributeSet);


            var qMgmt = LdapQueryManager.Instance;

            try
            {
                qMgmt.AddEntry(newEntry);
                return 0;

            }catch(Exception ex)
            {
                logger.Error("Error saving user");
                logger.Log(LogLevel.Error, ex);
                return -1;
            }

        }

        /// <summary>
        /// Saves the user.
        /// </summary>
        /// <returns>The user. Must have DN set</returns>
        /// <param name="person">User.</param>
        public int SavePerson(Person person)
        {

            var qMgmt = LdapQueryManager.Instance;

            var modList = new List<LdapModification>();

            var atributes = GetAttributeSet(person);

            //Get user from the Directory
            try
            {
                var duser = GetPerson(person.DN);

                var dattrs = GetAttributeSet(duser);

   
                foreach (LdapAttribute attr in atributes)
                {
                    
                    if (
                        attr.Name != "cn"
                        && attr.Name != "objectclass"
                        && attr.Name != "userPassword"
                      )
                    {

                        var b1 = attr.ByteValue;
                        var b2 = dattrs.GetAttribute(attr.Name).ByteValue;

                        var equal = ByteTools.Equality(b1, b2);
                                     
                        if (! equal)
                            modList.Add(new LdapModification(LdapModification.Replace, attr));
                    }

           
                }



                try
                {
                    qMgmt.SaveEntry(person.DN, modList.ToArray());
                    return 0;

                }
                catch (Exception ex)
                {
                    logger.Error("Error updating user");
                    logger.Log(LogLevel.Error, ex);
                    return -1;
                }

            }catch(Exception ex)
            {
                logger.Error("Error user not found");
                logger.Log(LogLevel.Error, ex);
                return -1;
            }


        }

        public bool ValidateAuthentication(string DN, string password)
        {

            LdapConnectionManager lcm = LdapConnectionManager.Instance;

            return lcm.ValidateAuthentication(DN, password);

        }


        private LdapAttributeSet GetAttributeSet(Person person)
        {
            LdapAttributeSet attributeSet = new LdapAttributeSet();

            attributeSet.Add(new LdapAttribute("objectclass", new string[] { "top", "person", "inetOrgPerson", "organizationalPerson", "simpleSecurityObject" }));
            attributeSet.Add(new LdapAttribute("cn", new string[] { person.Name }));
            if (person.Surname == null) person.Surname = "---";
            attributeSet.Add(new LdapAttribute("sn", person.Surname ));


            if (person.IsDisabled == true)
            {
                if (!person.Description.StartsWith("[DISABLED]"))
                    attributeSet.Add(new LdapAttribute("description", "[DISABLED]"+person.Description));
            }
            else
            {
                attributeSet.Add(new LdapAttribute("description", person.Description));
            }

            if (person.Password == null )
            {
                if(person.IsDisabled == null) person.IsDisabled = true;
            }
            else
            {
                if (person.IsDisabled == null) person.IsDisabled = false;
                var ldapCfg = new LdapConfig();
                /*if (ldapCfg.ssl == false)
                {
                    throw new domain.Exceptions.SSLRequiredException();
                }*/

                //string quotePwd = String.Format(@"""{0}""", user.Password);
                //byte[] encodedBytes = Encoding.Unicode.GetBytes(quotePwd);
                //attributeSet.Add(new LdapAttribute("unicodePwd", encodedBytes));

                var hashedPassword = lapi.Security.HashHelper.GenerateSaltedSHA1(person.Password);
                attributeSet.Add(new LdapAttribute("userPassword", hashedPassword));
                
            }

            if (person.Mails != null)
            {
                foreach (var mail in person.Mails)
                {
                    attributeSet.Add(new LdapAttribute("mail", mail));
                }
                
            }

            if (person.Phones != null)
            {
                foreach (var phone in person.Phones)
                {
                    attributeSet.Add(new LdapAttribute("homePhone", phone));
                }
            }
            
            if (person.Addresses != null)
            {
                foreach (var address in person.Addresses)
                {
                    attributeSet.Add(new LdapAttribute("street", address));
                }
            }
            
            if (person.Mobiles != null)
            {
                foreach (var mobile in person.Mobiles)
                {
                    attributeSet.Add(new LdapAttribute("mobile", mobile));
                }
            }
            
            if (person.IDs != null)
            {
                foreach (var id in person.IDs)
                {
                    attributeSet.Add(new LdapAttribute("uid", id));
                }
            }
            
            if(person.GivenName != null) 
                attributeSet.Add(new LdapAttribute("givenname", person.GivenName));
            
            if(person.State != null) 
                attributeSet.Add(new LdapAttribute("st", person.State));


            return attributeSet;
        }

        private Person ConvertfromLdap(LdapEntry entry)
        {
            var person = new Person();

            person.Name = entry.GetAttribute("cn").StringValue;

            if(entry.GetAttribute("description") != null) person.Description = entry.GetAttribute("description").StringValue;

            person.DN = entry.Dn;

            if (person.Description.StartsWith("[DISABLED]"))
            {
                person.IsDisabled = true;
                person.Description = person.Description.Substring("[DISABLED]".Length);
            }
            else person.IsDisabled = false;

            if (entry.GetAttribute("mail") != null)
            {
                var mails = entry.GetAttribute("mail").StringValues;

                person.Mails = new List<string>();
                
                while(mails.MoveNext())
                {
                    person.Mails.Add(mails.Current);
                }
            }
            
            if (entry.GetAttribute("homePhone") != null)
            {
                var phones = entry.GetAttribute("homePhone").StringValues;

                person.Phones = new List<string>();
                
                while(phones.MoveNext())
                {
                    person.Phones.Add(phones.Current);
                }
            }
            
            if (entry.GetAttribute("mobile") != null)
            {
                var mobiles = entry.GetAttribute("mobile").StringValues;

                person.Mobiles = new List<string>();
                
                while(mobiles.MoveNext())
                {
                    person.Mobiles.Add(mobiles.Current);
                }
            }
            
            if (entry.GetAttribute("street") != null)
            {
                var addresses = entry.GetAttribute("street").StringValues;

                person.Addresses = new List<string>();
                
                while(addresses.MoveNext())
                {
                    person.Addresses.Add(addresses.Current);
                }
            }
            
            if (entry.GetAttribute("uid") != null)
            {
                var ids = entry.GetAttribute("uid").StringValues;

                person.IDs = new List<string>();
                
                while(ids.MoveNext())
                {
                    person.IDs.Add(ids.Current);
                }
            }
           
            if (entry.GetAttribute("givenname") != null)
            {
                person.GivenName = entry.GetAttribute("givenname").StringValue;
            }
            
            if (entry.GetAttribute("st") != null)
            {
                person.State = entry.GetAttribute("st").StringValue;
            }

            if (entry.GetAttribute("sn") != null)
            {
                person.Surname = entry.GetAttribute("sn").StringValue;
            }
            

            return person;
        }

        /// <summary>
        /// Deletes the user.
        /// </summary>
        /// <returns>0 for success -1 for error.</returns>
        /// <param name="person">User.</param>
        public int DeleteUser(Person person)
        {
        

            var qMgmt = LdapQueryManager.Instance;

            try
            {
                qMgmt.DeleteEntry(person.DN);
                return 0;

            }
            catch (Exception ex)
            {
                logger.Error("Error deleting user");
                logger.Log(LogLevel.Error, ex);
                return -1;
            }

        }
    }
}
