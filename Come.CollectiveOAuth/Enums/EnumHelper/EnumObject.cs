using System;

namespace Come.CollectiveOAuth.Enums
{
    public struct EnumObject
    {
        public EnumObject(Enum um, string picture = null)
        {
            this.ID = (int)Convert.ChangeType(um, typeof(int));
            this.Name = um.ToString();
            this.Description = um.GetDesc();
            this.Picture = picture;
        }

        public EnumObject(int id, string name)
        {
            this.ID = id;
            this.Name = this.Description = name;
            this.Picture = null;
        }

        public EnumObject(int id, string name, string description, string picture)
        {
            this.ID = id;
            this.Name = name;
            this.Description = description;
            this.Picture = picture;
        }

        public int ID;

        public string Name;

        public string Description;

        public string Picture;
    }
}