using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Models
{
    [DataItem("Zookeeper")]
    public class Zookeeper : DataClass
    {
        private Guid _zookeeperId;
        private string _firstName;
        private string _lastName;
        private string _email;
        private DateTime _hireDate;
        private string _specialty;

        public Zookeeper() : base(true) { }
        public Zookeeper(bool addingNew) : base(addingNew) { }

        [DataField("ZookeeperId", DbType.Guid, IsKeyField = true)]
        public Guid ZookeeperId
        {
            get => _zookeeperId;
            set => SetFieldValue(ref _zookeeperId, value);
        }

        [DataField("FirstName", DbType.String)]
        public string FirstName
        {
            get => _firstName;
            set => SetFieldValue(ref _firstName, value);
        }

        [DataField("LastName", DbType.String)]
        public string LastName
        {
            get => _lastName;
            set => SetFieldValue(ref _lastName, value);
        }

        [DataField("Email", DbType.String, true)]
        public string Email
        {
            get => _email;
            set => SetFieldValue(ref _email, value);
        }

        [DataField("HireDate", DbType.Date)]
        public DateTime HireDate
        {
            get => _hireDate;
            set => SetFieldValue(ref _hireDate, value);
        }

        [DataField("Specialty", DbType.String, true)]
        public string Specialty
        {
            get => _specialty;
            set => SetFieldValue(ref _specialty, value);
        }
    }
}
