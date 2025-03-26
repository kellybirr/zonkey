#if (NET6_0_OR_GREATER)
#nullable enable
using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Enforcement.Data.Models
{
	[DataItem("license_plates")]
	public partial class EaLicensePlate : DataClass
	{
		#region Data Columns (Properties)

		[DataField("license_plate_id", DbType.Int64, false, IsKeyField = true)]
		public Int64 Id
		{
			get => _id;
			set => SetFieldValue(ref _id, value);
		}
		private Int64 _id;

		[DataField("license_plate_import_id", DbType.Int64, true)]
		public Int64? ImportId
		{
			get => _importId;
			set => SetFieldValue(ref _importId, value);
		}
		private Int64? _importId;

		[DataField("license_plate_user_id", DbType.Int64, true)]
		public Int64? UserId
		{
			get => _userId;
			set => SetFieldValue(ref _userId, value);
		}
		private Int64? _userId;

		[DataField("license_plate_license_lookup_service_id", DbType.Int64, true)]
		public Int64? LicenseLookupServiceId
		{
			get => _licenseLookupServiceId;
			set => SetFieldValue(ref _licenseLookupServiceId, value);
		}
		private Int64? _licenseLookupServiceId;

		[DataField("license_plate_plate", DbType.String, true)]
		public string? Plate
		{
			get => _plate;
			set => SetFieldValue(ref _plate, value);
		}
		private string? _plate;

		[DataField("license_plate_state_abbrev", DbType.String, true)]
		public string? StateAbbrev
		{
			get => _stateAbbrev;
			set => SetFieldValue(ref _stateAbbrev, value);
		}
		private string? _stateAbbrev;

		[DataField("license_plate_state_id", DbType.Int64, true)]
		public Int64? StateId
		{
			get => _stateId;
			set => SetFieldValue(ref _stateId, value);
		}
		private Int64? _stateId;

		[DataField("license_plate_type", DbType.String, true)]
		public string? Type
		{
			get => _type;
			set => SetFieldValue(ref _type, value);
		}
		private string? _type;

		[DataField("license_plate_created_timestamp", DbType.DateTime, true, DateTimeKind = DateTimeKind.Utc)]
		public DateTime? CreatedTimestamp
		{
			get => _createdTimestamp;
			set => SetFieldValue(ref _createdTimestamp, value);
		}
		private DateTime? _createdTimestamp;

		[DataField("license_plate_updated_timestamp", DbType.DateTime, true, DateTimeKind = DateTimeKind.Utc)]
		public DateTime? UpdatedTimestamp
		{
			get => _updatedTimestamp;
			set => SetFieldValue(ref _updatedTimestamp, value);
		}
		private DateTime? _updatedTimestamp;

		[DataField("license_plate_expires_timestamp", DbType.DateTime, true, DateTimeKind = DateTimeKind.Utc)]
		public DateTime? ExpiresTimestamp
		{
			get => _expiresTimestamp;
			set => SetFieldValue(ref _expiresTimestamp, value);
		}
		private DateTime? _expiresTimestamp;

		[DataField("license_plate_vehicle_vin", DbType.String, true)]
		public string? VehicleVin
		{
			get => _vehicleVin;
			set => SetFieldValue(ref _vehicleVin, value);
		}
		private string? _vehicleVin;

		[DataField("license_plate_vehicle_type", DbType.String, true)]
		public string? VehicleType
		{
			get => _vehicleType;
			set => SetFieldValue(ref _vehicleType, value);
		}
		private string? _vehicleType;

		[DataField("license_plate_vehicle_year", DbType.String, true)]
		public string? VehicleYear
		{
			get => _vehicleYear;
			set => SetFieldValue(ref _vehicleYear, value);
		}
		private string? _vehicleYear;

		[DataField("license_plate_vehicle_make", DbType.String, true)]
		public string? VehicleMake
		{
			get => _vehicleMake;
			set => SetFieldValue(ref _vehicleMake, value);
		}
		private string? _vehicleMake;

		[DataField("license_plate_vehicle_model", DbType.String, true)]
		public string? VehicleModel
		{
			get => _vehicleModel;
			set => SetFieldValue(ref _vehicleModel, value);
		}
		private string? _vehicleModel;

		[DataField("license_plate_vehicle_series", DbType.String, true)]
		public string? VehicleSeries
		{
			get => _vehicleSeries;
			set => SetFieldValue(ref _vehicleSeries, value);
		}
		private string? _vehicleSeries;

		[DataField("license_plate_vehicle_color", DbType.String, true)]
		public string? VehicleColor
		{
			get => _vehicleColor;
			set => SetFieldValue(ref _vehicleColor, value);
		}
		private string? _vehicleColor;

		[DataField("license_plate_vehicle_body", DbType.String, true)]
		public string? VehicleBody
		{
			get => _vehicleBody;
			set => SetFieldValue(ref _vehicleBody, value);
		}
		private string? _vehicleBody;

		[DataField("license_plate_registrant_first_name", DbType.String, true)]
		public string? RegistrantFirstName
		{
			get => _registrantFirstName;
			set => SetFieldValue(ref _registrantFirstName, value);
		}
		private string? _registrantFirstName;

		[DataField("license_plate_registrant_last_name", DbType.String, true)]
		public string? RegistrantLastName
		{
			get => _registrantLastName;
			set => SetFieldValue(ref _registrantLastName, value);
		}
		private string? _registrantLastName;

		[DataField("license_plate_registrant_address_1", DbType.String, true)]
		public string? RegistrantAddress1
		{
			get => _registrantAddress1;
			set => SetFieldValue(ref _registrantAddress1, value);
		}
		private string? _registrantAddress1;

		[DataField("license_plate_registrant_address_2", DbType.String, true)]
		public string? RegistrantAddress2
		{
			get => _registrantAddress2;
			set => SetFieldValue(ref _registrantAddress2, value);
		}
		private string? _registrantAddress2;

		[DataField("license_plate_registrant_city", DbType.String, true)]
		public string? RegistrantCity
		{
			get => _registrantCity;
			set => SetFieldValue(ref _registrantCity, value);
		}
		private string? _registrantCity;

		[DataField("license_plate_registrant_county", DbType.String, true)]
		public string? RegistrantCounty
		{
			get => _registrantCounty;
			set => SetFieldValue(ref _registrantCounty, value);
		}
		private string? _registrantCounty;

		[DataField("license_plate_registrant_state", DbType.String, true)]
		public string? RegistrantState
		{
			get => _registrantState;
			set => SetFieldValue(ref _registrantState, value);
		}
		private string? _registrantState;

		[DataField("license_plate_registrant_zip", DbType.String, true)]
		public string? RegistrantZip
		{
			get => _registrantZip;
			set => SetFieldValue(ref _registrantZip, value);
		}
		private string? _registrantZip;

		[DataField("license_plate_registrant_first_seen", DbType.DateTime, true)]
		public DateTime? RegistrantFirstSeen
		{
			get => _registrantFirstSeen;
			set => SetFieldValue(ref _registrantFirstSeen, value);
		}
		private DateTime? _registrantFirstSeen;

		[DataField("license_plate_registrant_last_seen", DbType.DateTime, true)]
		public DateTime? RegistrantLastSeen
		{
			get => _registrantLastSeen;
			set => SetFieldValue(ref _registrantLastSeen, value);
		}
		private DateTime? _registrantLastSeen;

		[DataField("license_plate_registrant_first_registered", DbType.DateTime, true)]
		public DateTime? RegistrantFirstRegistered
		{
			get => _registrantFirstRegistered;
			set => SetFieldValue(ref _registrantFirstRegistered, value);
		}
		private DateTime? _registrantFirstRegistered;

		[DataField("license_plate_registrant_last_registered", DbType.DateTime, true)]
		public DateTime? RegistrantLastRegistered
		{
			get => _registrantLastRegistered;
			set => SetFieldValue(ref _registrantLastRegistered, value);
		}
		private DateTime? _registrantLastRegistered;

		[DataField("license_plate_registrant2_first_name", DbType.String, true)]
		public string? Registrant2FirstName
		{
			get => _registrant2FirstName;
			set => SetFieldValue(ref _registrant2FirstName, value);
		}
		private string? _registrant2FirstName;

		[DataField("license_plate_registrant2_last_name", DbType.String, true)]
		public string? Registrant2LastName
		{
			get => _registrant2LastName;
			set => SetFieldValue(ref _registrant2LastName, value);
		}
		private string? _registrant2LastName;

		[DataField("license_plate_registrant2_address_1", DbType.String, true)]
		public string? Registrant2Address1
		{
			get => _registrant2Address1;
			set => SetFieldValue(ref _registrant2Address1, value);
		}
		private string? _registrant2Address1;

		[DataField("license_plate_registrant2_address_2", DbType.String, true)]
		public string? Registrant2Address2
		{
			get => _registrant2Address2;
			set => SetFieldValue(ref _registrant2Address2, value);
		}
		private string? _registrant2Address2;

		[DataField("license_plate_registrant2_city", DbType.String, true)]
		public string? Registrant2City
		{
			get => _registrant2City;
			set => SetFieldValue(ref _registrant2City, value);
		}
		private string? _registrant2City;

		[DataField("license_plate_registrant2_county", DbType.String, true)]
		public string? Registrant2County
		{
			get => _registrant2County;
			set => SetFieldValue(ref _registrant2County, value);
		}
		private string? _registrant2County;

		[DataField("license_plate_registrant2_state", DbType.String, true)]
		public string? Registrant2State
		{
			get => _registrant2State;
			set => SetFieldValue(ref _registrant2State, value);
		}
		private string? _registrant2State;

		[DataField("license_plate_registrant2_zip", DbType.String, true)]
		public string? Registrant2Zip
		{
			get => _registrant2Zip;
			set => SetFieldValue(ref _registrant2Zip, value);
		}
		private string? _registrant2Zip;

		[DataField("license_plate_registrant2_first_seen", DbType.DateTime, true)]
		public DateTime? Registrant2FirstSeen
		{
			get => _registrant2FirstSeen;
			set => SetFieldValue(ref _registrant2FirstSeen, value);
		}
		private DateTime? _registrant2FirstSeen;

		[DataField("license_plate_registrant2_last_seen", DbType.DateTime, true)]
		public DateTime? Registrant2LastSeen
		{
			get => _registrant2LastSeen;
			set => SetFieldValue(ref _registrant2LastSeen, value);
		}
		private DateTime? _registrant2LastSeen;

		[DataField("license_plate_registrant2_first_registered", DbType.DateTime, true)]
		public DateTime? Registrant2FirstRegistered
		{
			get => _registrant2FirstRegistered;
			set => SetFieldValue(ref _registrant2FirstRegistered, value);
		}
		private DateTime? _registrant2FirstRegistered;

		[DataField("license_plate_registrant2_last_registered", DbType.DateTime, true)]
		public DateTime? Registrant2LastRegistered
		{
			get => _registrant2LastRegistered;
			set => SetFieldValue(ref _registrant2LastRegistered, value);
		}
		private DateTime? _registrant2LastRegistered;

		[DataField("license_plate_registrant_company_name", DbType.String, true)]
		public string? RegistrantCompanyName
		{
			get => _registrantCompanyName;
			set => SetFieldValue(ref _registrantCompanyName, value);
		}
		private string? _registrantCompanyName;

		[DataField("license_plate_registrant_full_name", DbType.String, true)]
		public string? RegistrantFullName
		{
			get => _registrantFullName;
			set => SetFieldValue(ref _registrantFullName, value);
		}
		private string? _registrantFullName;

		[DataField("license_plate_registrant_middle_name", DbType.String, true)]
		public string? RegistrantMiddleName
		{
			get => _registrantMiddleName;
			set => SetFieldValue(ref _registrantMiddleName, value);
		}
		private string? _registrantMiddleName;

		[DataField("license_plate_registrant_suffix", DbType.String, true)]
		public string? RegistrantSuffix
		{
			get => _registrantSuffix;
			set => SetFieldValue(ref _registrantSuffix, value);
		}
		private string? _registrantSuffix;

		[DataField("license_plate_registrant_full_address", DbType.String, true)]
		public string? RegistrantFullAddress
		{
			get => _registrantFullAddress;
			set => SetFieldValue(ref _registrantFullAddress, value);
		}
		private string? _registrantFullAddress;

		[DataField("license_plate_registrant2_company_name", DbType.String, true)]
		public string? Registrant2CompanyName
		{
			get => _registrant2CompanyName;
			set => SetFieldValue(ref _registrant2CompanyName, value);
		}
		private string? _registrant2CompanyName;

		[DataField("license_plate_registrant2_full_name", DbType.String, true)]
		public string? Registrant2FullName
		{
			get => _registrant2FullName;
			set => SetFieldValue(ref _registrant2FullName, value);
		}
		private string? _registrant2FullName;

		[DataField("license_plate_registrant2_middle_name", DbType.String, true)]
		public string? Registrant2MiddleName
		{
			get => _registrant2MiddleName;
			set => SetFieldValue(ref _registrant2MiddleName, value);
		}
		private string? _registrant2MiddleName;

		[DataField("license_plate_registrant2_suffix", DbType.String, true)]
		public string? Registrant2Suffix
		{
			get => _registrant2Suffix;
			set => SetFieldValue(ref _registrant2Suffix, value);
		}
		private string? _registrant2Suffix;

		[DataField("license_plate_registrant2_full_address", DbType.String, true)]
		public string? Registrant2FullAddress
		{
			get => _registrant2FullAddress;
			set => SetFieldValue(ref _registrant2FullAddress, value);
		}
		private string? _registrant2FullAddress;

		[DataField("license_plate_registrant3_company_name", DbType.String, true)]
		public string? Registrant3CompanyName
		{
			get => _registrant3CompanyName;
			set => SetFieldValue(ref _registrant3CompanyName, value);
		}
		private string? _registrant3CompanyName;

		[DataField("license_plate_registrant3_full_name", DbType.String, true)]
		public string? Registrant3FullName
		{
			get => _registrant3FullName;
			set => SetFieldValue(ref _registrant3FullName, value);
		}
		private string? _registrant3FullName;

		[DataField("license_plate_registrant3_first_name", DbType.String, true)]
		public string? Registrant3FirstName
		{
			get => _registrant3FirstName;
			set => SetFieldValue(ref _registrant3FirstName, value);
		}
		private string? _registrant3FirstName;

		[DataField("license_plate_registrant3_middle_name", DbType.String, true)]
		public string? Registrant3MiddleName
		{
			get => _registrant3MiddleName;
			set => SetFieldValue(ref _registrant3MiddleName, value);
		}
		private string? _registrant3MiddleName;

		[DataField("license_plate_registrant3_last_name", DbType.String, true)]
		public string? Registrant3LastName
		{
			get => _registrant3LastName;
			set => SetFieldValue(ref _registrant3LastName, value);
		}
		private string? _registrant3LastName;

		[DataField("license_plate_registrant3_suffix", DbType.String, true)]
		public string? Registrant3Suffix
		{
			get => _registrant3Suffix;
			set => SetFieldValue(ref _registrant3Suffix, value);
		}
		private string? _registrant3Suffix;

		[DataField("license_plate_registrant3_full_address", DbType.String, true)]
		public string? Registrant3FullAddress
		{
			get => _registrant3FullAddress;
			set => SetFieldValue(ref _registrant3FullAddress, value);
		}
		private string? _registrant3FullAddress;

		[DataField("license_plate_registrant3_address_1", DbType.String, true)]
		public string? Registrant3Address1
		{
			get => _registrant3Address1;
			set => SetFieldValue(ref _registrant3Address1, value);
		}
		private string? _registrant3Address1;

		[DataField("license_plate_registrant3_address_2", DbType.String, true)]
		public string? Registrant3Address2
		{
			get => _registrant3Address2;
			set => SetFieldValue(ref _registrant3Address2, value);
		}
		private string? _registrant3Address2;

		[DataField("license_plate_registrant3_city", DbType.String, true)]
		public string? Registrant3City
		{
			get => _registrant3City;
			set => SetFieldValue(ref _registrant3City, value);
		}
		private string? _registrant3City;

		[DataField("license_plate_registrant3_county", DbType.String, true)]
		public string? Registrant3County
		{
			get => _registrant3County;
			set => SetFieldValue(ref _registrant3County, value);
		}
		private string? _registrant3County;

		[DataField("license_plate_registrant3_state", DbType.String, true)]
		public string? Registrant3State
		{
			get => _registrant3State;
			set => SetFieldValue(ref _registrant3State, value);
		}
		private string? _registrant3State;

		[DataField("license_plate_registrant3_zip", DbType.String, true)]
		public string? Registrant3Zip
		{
			get => _registrant3Zip;
			set => SetFieldValue(ref _registrant3Zip, value);
		}
		private string? _registrant3Zip;

		[DataField("license_plate_submitted_plate", DbType.String, true)]
		public string? SubmittedPlate
		{
			get => _submittedPlate;
			set => SetFieldValue(ref _submittedPlate, value);
		}
		private string? _submittedPlate;

		[DataField("license_plate_submitted_state_abbrev", DbType.String, true)]
		public string? SubmittedStateAbbrev
		{
			get => _submittedStateAbbrev;
			set => SetFieldValue(ref _submittedStateAbbrev, value);
		}
		private string? _submittedStateAbbrev;

		[DataField("license_plate_submitted_state_id", DbType.Int64, true)]
		public Int64? SubmittedStateId
		{
			get => _submittedStateId;
			set => SetFieldValue(ref _submittedStateId, value);
		}
		private Int64? _submittedStateId;

		[DataField("license_plate_submitted_vehicle_vin", DbType.String, true)]
		public string? SubmittedVehicleVin
		{
			get => _submittedVehicleVin;
			set => SetFieldValue(ref _submittedVehicleVin, value);
		}
		private string? _submittedVehicleVin;

		[DataField("license_plate_submitted_vehicle_make", DbType.String, true)]
		public string? SubmittedVehicleMake
		{
			get => _submittedVehicleMake;
			set => SetFieldValue(ref _submittedVehicleMake, value);
		}
		private string? _submittedVehicleMake;

		[DataField("license_plate_mismatch_reason", DbType.String, true)]
		public string? MismatchReason
		{
			get => _mismatchReason;
			set => SetFieldValue(ref _mismatchReason, value);
		}
		private string? _mismatchReason;

		[DataField("license_plate_hold_status", DbType.Int16, true)]
		public Int16? HoldStatus
		{
			get => _holdStatus;
			set => SetFieldValue(ref _holdStatus, value);
		}
		private Int16? _holdStatus;

		#endregion

		#region Constructors

		public EaLicensePlate(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public EaLicensePlate() : this(false)
		{ }

		#endregion

	}

}
#endif