﻿using System;
using System.Collections.Generic;
using Novell.Directory.Ldap;
using lapi.domain;
using lapi.Ldap;
using lapi.Tools;
using NLog;

namespace lapi
{
    public class GroupManager: ObjectManager
    {

        #region SINGLETON

        private static readonly Lazy<GroupManager> lazy = new Lazy<GroupManager>(() => new GroupManager());

        public static GroupManager Instance { get { return lazy.Value; } }

        private GroupManager()
        {
            logger = NLog.LogManager.GetCurrentClassLogger();
        }

        #endregion

        
        /// <summary>
        /// Return a string list of the groups DNs
        /// </summary>
        /// <returns>The list.</returns>
        /// <param name="searchBase"> The OU to be added in the search </param>
        public List<string> GetList(string searchBase = "")
        {
            var groups = new List<String>();

            var sMgmt = LdapQueryManager.Instance;

            int results = 0;


            var resps = sMgmt.ExecuteSearch(searchBase, LdapSearchType.Group);

            foreach (var entry in resps)
            {
                groups.Add(entry.Dn);
                results++;
            }

            logger.Debug("Group search executed results:{result}", results);


            return groups;
        }

        //TODO: Verify the lower limit witch is not working
        /// <summary>
        /// Gets the list, based on a start and end
        /// </summary>
        /// <returns>The list.</returns>
        /// <param name="start">Start.</param>
        /// <param name="end">End.</param>
        /// <param name="searchBase"> The OU to be added in the search </param>
        public List<string> GetList( int start, int end, string searchBase = "")
        {
            var groups = new List<String>();

            var sMgmt = LdapQueryManager.Instance;

            int results = 0;


            var resps = sMgmt.ExecuteLimitedSearch(searchBase, LdapSearchType.Group, start, end);

            foreach (var entry in resps)
            {
                groups.Add(entry.Dn);
                results++;
            }

            logger.Debug("Group search executed results:{result}", results);


            return groups;
        }

        /// <summary>
        /// Gets the list of all groups.
        /// </summary>
        /// <returns>The users.</returns>
        public List<Group> GetGroups(string searchBase = "")
        {

            var groups = new List<Group>();

            var sMgmt = LdapQueryManager.Instance;

            var resps = sMgmt.ExecuteSearch(searchBase, LdapSearchType.Group);
            int results = 0;

            foreach (var entry in resps)
            {
                groups.Add(ConvertfromLdap(entry));
                results++;
            }

            logger.Debug("Group search executed results:{result}", results);


            return groups;
        }

        /// <summary>
        /// Converts the data from the LDAP result
        /// </summary>
        /// <returns>The LDAP.</returns>
        /// <param name="entry">Entry.</param>
        private Group ConvertfromLdap(LdapEntry entry)
        {
            var group = new Group();

            group.Name = entry.GetAttribute("cn").StringValue;
          
            if (entry.GetAttribute("description") != null) group.Description = entry.GetAttribute("description").StringValue;

            //var sid = ConvertByteToStringSid((byte[])(Array)entry.GetAttribute("objectSid").ByteValue);

            //group.ID = sid;

            group.DN = entry.Dn;


            if (entry.GetAttribute("member") != null)
            {
                var m = entry.GetAttribute("member").StringValues;

                while (m.MoveNext())
                {
                    String member = "";
                    if (m != null && m.Current != null)
                    {
                        member = m.Current;
                        group.Member.Add(member);
                    }
                }
            }
            
            if (entry.GetAttribute("uniqueMember") != null)
            {
                var m = entry.GetAttribute("uniqueMember").StringValues;

                while (m.MoveNext())
                {
                    String member = "";
                    if (m != null && m.Current != null)
                    {
                        member = m.Current;
                        group.Member.Add(member);
                    }
                }
            }
            
            var classes = entry.GetAttribute("objectClass").StringValues;

            bool is_regular = false;
            while (classes.MoveNext())
            {
                if (classes.Current == "groupOfNames")
                {
                    group.Type = GroupType.Regular;
                    is_regular = true;
                }
            }

            if (!is_regular) group.Type = GroupType.Unique;
            

            return group;
        }

        /// <summary>
        /// Gets the group.
        /// </summary>
        /// <returns>The user.</returns>
        /// <param name="DN">The Disitnguesh name of the group</param>
        public Group GetGroup(string DN)
        {
            var sMgmt = LdapQueryManager.Instance;

            try
            {
                var entry = sMgmt.GetRegister(DN);
                var group = ConvertfromLdap(entry);
                return group;
            }
            catch (LdapException ex)
            {
                logger.Debug("User not found {0} Ex: {1}", DN, ex.Message);
                return null;
            }

        }

        public int CreateGroup(Group group)
        {

            //Creates the List attributes of the entry and add them to attributeset

            LdapAttributeSet attributeSet = GetAttributeSet(group);

            // DN of the entry to be added
            string dn = group.DN;

            LdapEntry newEntry = new LdapEntry(dn, attributeSet);

            var qMgmt = LdapQueryManager.Instance;

            try
            {
                qMgmt.AddEntry(newEntry);
                return 0;

            }
            catch (Exception ex)
            {
                logger.Error("Error saving group");
                logger.Log(LogLevel.Error, ex);
                return -1;
            }

        }

        /// <summary>
        /// Saves the group.
        /// </summary>
        /// <returns>The group. Must have DN set</returns>
        /// <param name="group">Group.</param>
        public int SaveGroup(Group group)
        {

            var qMgmt = LdapQueryManager.Instance;

            var modList = new List<LdapModification>();

            var atributes = GetAttributeSet(group);

            //Get user from the Directory
            try
            {
                var dgroup = GetGroup(group.DN);

                var dattrs = GetAttributeSet(dgroup);

                bool members_clean = false;

                foreach (LdapAttribute attr in atributes)
                {
                    if (
                        attr.Name != "cn"
                        && attr.Name != "objectclass"
                        && attr.Name != "member"
                      )
                    {

                        var b1 = attr.ByteValue;

                        var attribute = dattrs.GetAttribute(attr.Name);

                        bool equal = true;

                        if (attribute != null)
                        {
                            var b2 = attribute.ByteValue;
                            
                            equal = ByteTools.Equality(b1, b2);
                        }


                        if (!equal)
                            modList.Add(new LdapModification(LdapModification.Replace, attr));
                    }
                    else
                    {
                        if(attr.Name == "member")
                        {
                            if (!members_clean)
                            {
                                var dattr = dattrs.GetAttribute("member");

                                modList.Add(new LdapModification(LdapModification.Delete, dattr));

                                members_clean = true;
                            }


                            modList.Add(new LdapModification(LdapModification.Add, attr));
                        }
                    }


                }


                try
                {
                    qMgmt.SaveEntry(group.DN, modList.ToArray());
                    return 0;

                }
                catch (Exception ex)
                {
                    logger.Error("Error updating group");
                    logger.Log(LogLevel.Error, ex);
                    return -1;
                }

            }
            catch (Exception ex)
            {
                logger.Error("Error group not found");
                logger.Log(LogLevel.Error, ex);
                return -1;
            }


        }

        private LdapAttributeSet GetAttributeSet(Group group)
        {
            LdapAttributeSet attributeSet = new LdapAttributeSet();

            attributeSet.Add(new LdapAttribute("objectclass", new string[] { "top", "groupOfNames" }));
            //attributeSet.Add(new LdapAttribute("name", group.Name));
            attributeSet.Add(new LdapAttribute("cn", group.Name));
            if(group.Description != null)
                attributeSet.Add(new LdapAttribute("description", group.Description));

            if(group.Member.Count == 0) group.Member.Add("");
                
            var amember = new LdapAttribute("member");

            foreach (String member in group.Member)
            {
                amember.AddValue(member);
            }
             

            attributeSet.Add(amember);
            

            return attributeSet;
        }


        public int DeleteGroup(Group group)
        {


            var qMgmt = LdapQueryManager.Instance;

            try
            {
                qMgmt.DeleteEntry(group.DN);
                return 0;

            }
            catch (Exception ex)
            {
                logger.Error("Error deleting group={group}", group.DN);
                logger.Log(LogLevel.Error, ex);
                return -1;
            }

        }

    }
}
