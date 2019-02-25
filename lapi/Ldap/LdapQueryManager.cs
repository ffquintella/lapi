using System;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Controls;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace lapi.Ldap
{
    public class LdapQueryManager
    {
        public NLog.Logger logger;

        private LdapConfig config;

        #region SINGLETON

        private static readonly Lazy<LdapQueryManager> lazy = new Lazy<LdapQueryManager>(() => new LdapQueryManager());

        public static LdapQueryManager Instance { get { return lazy.Value; } }

        private LdapQueryManager()
        {
            logger = NLog.LogManager.GetCurrentClassLogger();
            config = new Ldap.LdapConfig();
        }
        #endregion

        #region READ
        public LdapMessageQueue SendSearch(string searchBase, LdapSearchType type)
        {
            switch (type) {
                case LdapSearchType.User:
                    logger.Debug("Searching all users");
                    return SendSearch(searchBase, $"(objectClass=person)");
                case LdapSearchType.Group:
                    logger.Debug("Searching all groups");
                    return SendSearch(searchBase, $"(objectClass=group)");
                case LdapSearchType.OU:
                    logger.Debug("Searching all OUs");
                    return SendSearch(searchBase, $"(objectClass=organizationalUnit)");
                case LdapSearchType.Machine:
                    logger.Debug("Searching all computers");
                    return SendSearch(searchBase, $"(objectClass=computer)");
                default:
                    logger.Error("Search type not specified.");
                    throw new domain.Exceptions.WrongParameterException("Search type not specified");
            }
        }

        public LdapMessageQueue SendSearch(string searchBase, string filter)
        {
            var lcm = LdapConnectionManager.Instance;
            var con = lcm.GetConnection();

            var sb = searchBase + config.searchBase;

            var req = new LdapSearchRequest(sb, LdapConnection.ScopeSub, filter, null, LdapSearchConstraints.DerefNever, config.maxResults, 0, false, null);
            
            var queue = con.SendRequest(req, null);
                        
            return queue;

        }

        public List<LdapEntry> ExecuteSearch(string searchBase, LdapSearchType type)
        {
            switch (type)
            {
                case LdapSearchType.User:
                    logger.Debug("Searching all users");
                    return ExecuteSearch(searchBase, $"(objectClass=person)");
                case LdapSearchType.Group:
                    logger.Debug("Searching all groups");
                    return ExecuteSearch(searchBase, $"(|(objectClass=groupOfNames)(objectClass=groupOfUniqueNames))");
                case LdapSearchType.OU:
                    logger.Debug("Searching all OUs");
                    return ExecuteSearch(searchBase, $"(objectClass=organizationalUnit)");
                case LdapSearchType.Machine:
                    logger.Debug("Searching all computers");
                    return ExecuteSearch(searchBase, $"(objectClass=computer)");
                default:
                    logger.Error("Search type not specified.");
                    throw new domain.Exceptions.WrongParameterException("Search type not specified");
            }
        }
        
        public List<LdapEntry> ExecuteSearch(string searchBase, string filter)
        {
            var results = new List<LdapEntry>();

            var lcm = LdapConnectionManager.Instance;
            var conn = lcm.GetConnection();

            var sb = searchBase + config.searchBase;

            //string[] attrs = {"createtimestamp", null};

            // Send the search request - Synchronous Search is being used here 
            logger.Debug("Calling Asynchronous Search...");
            ILdapSearchResults res = (LdapSearchResults)conn.Search(sb, LdapConnection.ScopeSub, filter, null, false, (LdapSearchConstraints)null);

            // Loop through the results and print them out
            while (res.HasMore())
            {

                /* Get next returned entry.  Note that we should expect a Ldap-
                *Exception object as well just in case something goes wrong
                */
                LdapEntry nextEntry = null;
                try
                {
                    nextEntry = res.Next();
                    results.Add(nextEntry);
                }
                catch (Exception e)
                {
                    if (e is LdapReferralException)
                        continue;
                    else
                    {
                        logger.Error("Search stopped with exception " + e.ToString());
                        break;
                    }
                }

                /* Print out the returned Entries distinguished name.  */
                logger.Debug(nextEntry.Dn);

            }

            return results;
        }
        
        public List<LdapEntry> ExecuteLimitedSearch(string searchBase, LdapSearchType type, int start, int end)
        {
            switch (type)
            {
                case LdapSearchType.User:
                    logger.Debug("Searching all users");
                    return ExecuteLimitedSearch(searchBase, $"(objectClass=person)", start, end);
                case LdapSearchType.Group:
                    logger.Debug("Searching all groups");
                    return ExecuteLimitedSearch(searchBase, $"(|(objectClass=groupOfNames)(objectClass=groupOfUniqueNames))", start, end);
                case LdapSearchType.OU:
                    logger.Debug("Searching all OUs");
                    return ExecuteLimitedSearch(searchBase, $"(objectClass=organizationalUnit)", start, end);
                case LdapSearchType.Machine:
                    logger.Debug("Searching all computers");
                    return ExecuteLimitedSearch(searchBase, $"(objectClass=computer)", start, end);
                default:
                    logger.Error("Searching type not specified.");
                    throw new domain.Exceptions.WrongParameterException("Search type not specified");
            }
        }

        /// <summary>
        /// Executes the limited search.
        /// </summary>
        /// <returns>The limited search.</returns>
        /// <param name="searchBase">Search base.</param>
        /// <param name="filter">Filter.</param>
        /// <param name="start">Must be 1 or greater</param>
        /// <param name="end">End.</param>
        public List<LdapEntry> ExecuteLimitedSearch(string searchBase, string filter, int start, int end)
        {
            var results = new List<LdapEntry>();

            var lcm = LdapConnectionManager.Instance;
            var conn = lcm.GetConnection();

            var sb = searchBase + config.searchBase;

            LdapControl[] requestControls = new LdapControl[2];

            LdapSortKey[] keys = new LdapSortKey[1];
            keys[0] = new LdapSortKey("name");

            // Create the sort control 
            requestControls[0] = new LdapSortControl(keys, true);


            requestControls[1] = new LdapVirtualListControl(start,
                                     0, end, config.maxResults);

            // Set the controls to be sent as part of search request
            LdapSearchConstraints cons = conn.SearchConstraints;
            cons.SetControls(requestControls);
            conn.Constraints = cons;


            // Send the search request - Synchronous Search is being used here 
            logger.Debug("Calling Asynchronous Search...");
            ILdapSearchResults res = (LdapSearchResults)conn.Search(sb, LdapConnection.ScopeSub, filter, null, false, (LdapSearchConstraints)null);

            // Loop through the results and print them out
            while (res.HasMore())
            {

                /* Get next returned entry.  Note that we should expect a Ldap-
                *Exception object as well just in case something goes wrong
                */
                LdapEntry nextEntry = null;
                try
                {
                    nextEntry = res.Next();
                    results.Add(nextEntry);
                }
                catch (Exception e)
                {
                    if (e is LdapReferralException)
                        continue;
                    else
                    {
                        logger.Error("Search stopped with exception " + e.ToString());
                        break;
                    }
                }

                /* Print out the returned Entries distinguished name.  */
                logger.Debug(nextEntry.Dn);

            }

            return results;
        }


        public LdapEntry GetRegister(string DN)
        {
            var lcm = LdapConnectionManager.Instance;
            var con = lcm.GetConnection(true);

            //string[] attrs = {"*", null};

            //var lsc = new LdapSearchConstraints(); 
            try
            {
                var res = con.Read(DN);
                return res;
            }
            catch (LdapException ex)
            {
                logger.Error("Could not read entry DN: {0} Message:{1}",DN, ex.ToString());
                return null;
            }

            

        }
        
        public LdapEntry GetTimeStamps(string DN)
        {
            var lcm = LdapConnectionManager.Instance;
            var con = lcm.GetConnection(true);

            string[] attrs = {"createtimestamp", "modifyTimeStamp"};
            
            var res = con.Read(DN, attrs);

            return res;

        }
        

        #endregion

        #region WRITE

        public void AddEntry(LdapEntry entry)
        {
            var lcm = LdapConnectionManager.Instance;
            var con = lcm.GetConnection(true);

            //Add the entry to the directory
            con.Add(entry);
            

            return;
        }

        public void DeleteEntry(String dn)
        {
            var lcm = LdapConnectionManager.Instance;
            var con = lcm.GetConnection(true);

            con.Delete(dn);

            return;
        }

        public void SaveEntry(String dn, LdapModification[] modList)
        {
            var lcm = LdapConnectionManager.Instance;
            var con = lcm.GetConnection(true);

            //Add the entry to the directory
            con.Modify(dn, modList);

            return;
        }

        #endregion


    }
}
