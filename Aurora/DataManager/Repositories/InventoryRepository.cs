using System;
using System.Collections.Generic;
using System.Diagnostics;
using NHibernate;
using NHibernate.Criterion;
using OpenMetaverse;
using Aurora.Framework;
using InventoryFolder = Aurora.Framework.InventoryFolder;

namespace Aurora.DataManager.Repositories
{
    public class InventoryRepository : DataManagerRepository, IInventoryData
    {
        public InventoryRepository(DataSessionProvider sessionProvider) : base(sessionProvider) { }
        public IList<InventoryObjectType> AllInventoryObjectTypes = new List<InventoryObjectType>();

        public IList<InventoryItem> GetActiveInventoryItemsByType(InventoryObjectType gestureType)
        {
            return new List<InventoryItem>();
        }

        /// <summary>
        /// Gets the folder that contains all folders.
        /// ParentID of this folder is UUID.Zero.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public InventoryFolder GetRootFolder(UUID user)
        {
            using (var session = OpenSession())
            {
                var rootFolders = session.CreateCriteria(typeof(InventoryFolder)).Add(Expression.IsNull("ParentFolder")).List<InventoryFolder>();
                if (rootFolders.Count > 0)
                {
                    Debug.Assert(rootFolders.Count == 1, "This is unexpected that we have more than one root folder.");
                    return rootFolders[0];
                }
            }
            return null;
        }

        public InventoryFolder CreateRootFolderAndSave(UUID owner, string folderRootName)
        {
            using (var session = OpenSession())
            {
                var folder = new InventoryFolder();
                folder.Owner = owner.ToString();
                folder.Name = folderRootName;
                folder.FolderId = UUID.Random().ToString();
                session.SaveOrUpdate(folder);
                return folder;
            }
        }

        public InventoryFolder CreateFolderAndSave(string folderName, InventoryFolder parentFolder)
        {
            using (var session = OpenSession())
            {
                var folder = new InventoryFolder();
                folder.Owner = parentFolder.Owner.ToString();
                folder.Name = folderName;
                folder.ParentFolder = parentFolder;
                folder.FolderId = UUID.Random().ToString();
                session.SaveOrUpdate(folder);
                return folder;
            }
        }

        public IList<InventoryFolder> GetSubfoldersWithAnyAssetPreferences(InventoryFolder rootFolder)
        {
            using (var session = OpenSession())
            {
                return session.CreateCriteria(typeof(InventoryFolder)).Add(Expression.Eq("ParentFolder", rootFolder)).Add(Expression.IsNotNull("PreferredAssetType")).List<InventoryFolder>();
            }
        }

        /// <summary>
        /// Gets all folders that are in the root folder.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public IList<InventoryFolder> GetMainFolders(UUID user)
        {
            using (var session = OpenSession())
            {
                return session.CreateCriteria(typeof(InventoryFolder)).Add(Expression.Eq("ParentFolder", GetRootFolder(user))).List<InventoryFolder>();
            }
        }

        /// <summary>
        /// Gets the first root folder that has the given name.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public InventoryFolder GetMainFolderByName(UUID user, string name)
        {
            using (var session = OpenSession())
            {
                var rootSubFolders = session.CreateCriteria(typeof(InventoryFolder)).Add(Expression.Eq("ParentFolder", GetRootFolder(user))).Add(Expression.Eq("Name", name)).List<InventoryFolder>();
                if (rootSubFolders.Count > 0)
                {
                    return rootSubFolders[0];
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new folder with the given name under the given folder.
        /// If the ObjectType.Type == 0, it is a normal folder with no PreferredAssetType.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parentFolder"></param>
        /// <param name="preferredType"></param>
        /// <returns></returns>
        public InventoryFolder CreateFolderUnderFolderAndSave(string folderName, InventoryFolder parentFolder, int preferredType)
        {
            using (var session = OpenSession())
            {
                var folder = new InventoryFolder();
                folder.Owner = parentFolder.Owner.ToString();
                folder.Name = folderName;
                folder.ParentFolder = parentFolder;
                folder.PreferredAssetType = preferredType;
                session.SaveOrUpdate(folder);
                return folder;
            }
        }

        /// <summary>
        /// Gets the folder the item is in
        /// </summary>
        /// <param name="baseItem"></param>
        /// <returns></returns>
        public InventoryFolder GetParentFolder(OpenSim.Framework.InventoryItemBase baseItem)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the folder the folder is in.
        /// Returns null if the root folder is queried.
        /// </summary>
        /// <param name="baseFolder"></param>
        /// <returns></returns>
        public InventoryFolder GetParentFolder(OpenSim.Framework.InventoryFolderBase baseFolder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the parentFolder to a new ID.
        /// </summary>
        /// <param name="IFFolder"></param>
        public void UpdateParentFolder(InventoryFolder IFFolder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes all given folders of the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="folderIds"></param>
        public void RemoveFolders(List<UUID> folderIds)
        {
            using (var session = OpenSession())
            {
                var rootSubFolders = session.CreateCriteria(typeof(InventoryFolder)).List<InventoryFolder>();
                foreach (var inventoryFolder in rootSubFolders)
                {
                    if (folderIds.Contains(UUID.Parse(inventoryFolder.FolderId)))
                    {
                        session.Delete(inventoryFolder);
                    }
                }
            }
        }

        /// <summary>
        /// Removes all the sub units of the given folder.
        /// </summary>
        /// <param name="folder"></param>
        public void RemoveFoldersAndItems(OpenSim.Framework.InventoryFolderBase folder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets all folders of the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IList<InventoryFolder> GetAllFolders(UUID userId)
        {
            using (var session = OpenSession())
            {
                return session.CreateCriteria(typeof(InventoryFolder)).Add(Expression.Eq("ParentFolder", GetRootFolder(userId))).List<InventoryFolder>();
            }
        }

        #region IInventoryData

        /// <summary>
        /// Adds a new InventoryObjectType to the repository.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="assetType"></param>
        /// <returns></returns>
        public bool AssignNewInventoryType(string name, int assetType)
        {
            if (AllInventoryObjectTypes.Contains(GetInventoryObjectTypeByType(assetType)))
                return false;
            CreateInventoryType(name, assetType);
            return true;
        }

        public InventoryObjectType CreateInventoryType(string name, int assetType)
        {
            InventoryObjectType type = new InventoryObjectType();
            type.Name = name;
            type.Type = assetType;
            AllInventoryObjectTypes.Add(type);
            return type;
        }

        /// <summary>
        /// Gets the InventoryObjectType from its type identifier.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public InventoryObjectType GetInventoryObjectTypeByType(int type)
        {
            foreach (InventoryObjectType ot in AllInventoryObjectTypes)
            {
                if (ot.Type == type)
                    return ot;
            }
            return null;
        }

        /// <summary>
        /// Gets all the current InventoryObjectTypes.
        /// </summary>
        /// <returns></returns>
        public IList<InventoryObjectType> GetAllInventoryTypes()
        {
            return AllInventoryObjectTypes;
        }

        #endregion

        public IList<InventoryFolder> GetChildFolders(InventoryFolder parentFolder)
        {
            using (var session = OpenSession())
            {
                return session.CreateCriteria(typeof(InventoryFolder)).Add(Expression.Eq("ParentFolder", parentFolder)).List<InventoryFolder>();
            }
        }

        public IList<InventoryFolder> GetUserFolders(UUID userId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrives the InventoryFolder by its ID.
        /// </summary>
        /// <param name="inventoryFolder"></param>
        /// <returns></returns>
        public InventoryFolder GetFolder(InventoryFolder inventoryFolder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrives the InventoryItem by its ID.
        /// </summary>
        /// <param name="inventoryItem"></param>
        /// <returns></returns>
        public InventoryItem GetItem(InventoryItem inventoryItem)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the parentID, name, and owner
        /// </summary>
        /// <param name="folder"></param>
        public void UpdateFolder(InventoryFolder folder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes all InventoryItems with the specified UUIDs.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="itemIDs"></param>
        public void RemoveItems(UUID userId, List<UUID> itemIDs)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new InventoryItem based off the InventoryItem given.
        /// </summary>
        /// <param name="inventoryItem"></param>
        public void CreateItem(InventoryItem inventoryItem)
        {
            throw new NotImplementedException();
        }

        public void CreateItems(IList<InventoryItem> inventoryItem)
        {
            throw new NotImplementedException();
        }

        public void UpdateItem(InventoryItem inventoryItem)
        {
            throw new NotImplementedException();
        }

        public void UpdateItems(IList<InventoryItem> inventoryItem)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new item with a new UUID, but has a parentItem
        /// </summary>
        /// <param name="item"></param>
        public void CreateLinkedItem(InventoryItem item)
        {
            throw new NotImplementedException();
        }

        public List<InventoryItem> GetItemsInFolder(InventoryFolder inventoryFolder)
        {
            throw new NotImplementedException();
        }
    }
}