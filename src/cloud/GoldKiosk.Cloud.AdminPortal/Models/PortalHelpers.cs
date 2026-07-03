using Microsoft.AspNetCore.Mvc.Rendering;

namespace GoldKiosk.Cloud.AdminPortal.Models;

/// <summary>Portal helpers.</summary>
public static class PortalHelpers
{
    /// <summary>Get is active dropdown.</summary>
    public static List<SelectListItem> GetIsActiveDropdown()
    {
        var list = new List<SelectListItem>
        {

            new SelectListItem { Text = "Active", Value = "true" },
            new SelectListItem { Text = "InActive", Value = "false" },
        };
        return list;
    }
    /// <summary>Get options dropdown.</summary>
    public static List<SelectListItem> GetOptionsDropdown()
    {
        var list = new List<SelectListItem>
        {

            new SelectListItem { Text = "Option A", Value = "a" },
            new SelectListItem { Text = "Option B", Value = "b" },
            new SelectListItem { Text = "Option C", Value = "c" },
            new SelectListItem { Text = "Option D", Value = "d" },
        };
        return list;
    }
    /// <summary>Get kiosk status dropdown.</summary>
    public static List<SelectListItem> GetKioskStatusDropdown()
    {
        // Values must match the CHECK constraint on kiosk.kiosks.status
        // (see db/0014_tables_kiosk.sql): onboarding | active | maintenance | offline | decommissioned.
        var list = new List<SelectListItem>
        {
            new SelectListItem { Text = "Onboarding",     Value = "onboarding" },
            new SelectListItem { Text = "Active",         Value = "active" },
            new SelectListItem { Text = "Maintenance",    Value = "maintenance" },
            new SelectListItem { Text = "Offline",        Value = "offline" },
            new SelectListItem { Text = "Decommissioned", Value = "decommissioned" },
        };
        return list;
    }
    // Values must match the CHECK on merchant.merchants.kyc_status
    // (see db/0023_tables_merchant.sql): pending | approved | rejected | review.
    /// <summary>Get KYC status dropdown.</summary>
    public static List<SelectListItem> GetKycStatusDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Pending",  Value = "pending" },
            new SelectListItem { Text = "Approved", Value = "approved" },
            new SelectListItem { Text = "Rejected", Value = "rejected" },
            new SelectListItem { Text = "Review",   Value = "review" },
        };
    }

    // Values must match the CHECK on merchant.wallet_topup_requests.status
    // (see db/0023_tables_merchant.sql): pending | approved | rejected | cancelled.
    /// <summary>Get wallet topup status dropdown.</summary>
    public static List<SelectListItem> GetWalletTopupStatusDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Pending",   Value = "pending" },
            new SelectListItem { Text = "Approved",  Value = "approved" },
            new SelectListItem { Text = "Rejected",  Value = "rejected" },
            new SelectListItem { Text = "Cancelled", Value = "cancelled" },
        };
    }

    // Values must match the CHECK on voucher.vouchers.discount_type
    // (see db/0024_tables_voucher.sql): percent | fixed.
    /// <summary>Get voucher discount type dropdown.</summary>
    public static List<SelectListItem> GetVoucherDiscountTypeDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Percent", Value = "percent" },
            new SelectListItem { Text = "Fixed",   Value = "fixed" },
        };
    }

    /// <summary>Get maintenance dropdown.</summary>
    public static List<SelectListItem> GetMaintenanceDropdown()
    {
        var list = new List<SelectListItem>
        {

            new SelectListItem { Text = "Yes", Value = "true" },
            new SelectListItem { Text = "No", Value = "false" },
        };
        return list;
    }

    /// <summary>Get feedback dropdown.</summary>
    public static List<SelectListItem> GetFeedbackDropdown()
    {

        var list = new List<SelectListItem>
        {
            new SelectListItem { Text = "Read" , Value = "true"},
            new SelectListItem { Text = "Unread" , Value = "false" }
        };

        return list;
    }

    /// <summary>Get pass fail dropdown.</summary>
    public static List<SelectListItem> GetPassFailDropdown()
    {

        var list = new List<SelectListItem>
        {
            new SelectListItem { Text = "Pass" , Value = "Passed"},
            new SelectListItem { Text = "Fail" , Value = "Failed" }
        };

        return list;
    }
    /// <summary>Get test pass fail dropdown.</summary>
    public static List<SelectListItem> GetTestPassFailDropdown()
    {

        var list = new List<SelectListItem>
        {
            new SelectListItem { Text = "Pass" , Value = "true"},
            new SelectListItem { Text = "Fail" , Value = "false" }
        };

        return list;
    }
    /// <summary>Get input type dropdown.</summary>
    public static List<SelectListItem> GetInputTypeDropdown()
    {
        var list = new List<SelectListItem>
        {

            new SelectListItem { Text = "Text", Value = "text" },
            new SelectListItem { Text = "Number", Value = "number" },
            new SelectListItem { Text = "Email", Value = "email" },
            new SelectListItem { Text = "Password", Value = "password" },
            new SelectListItem { Text = "Telephone", Value = "tel" },
            new SelectListItem { Text = "DropDown", Value = "dropdown" },
            new SelectListItem { Text = "SmartCard", Value = "smartcard" },
            new SelectListItem { Text = "Camera", Value = "camera" }
        };
        return list;
    }

    /// <summary>Get language dropdown.</summary>
    public static List<SelectListItem> GetLanguageDropdown()
    {

        var list = new List<SelectListItem>
        {
            new SelectListItem { Text = "English" , Value = "en"}
        };

        return list;
    }
    /// <summary>Get schedule type dropdown.</summary>
    public static List<SelectListItem> GetScheduleTypeDropdown()
    {

        var list = new List<SelectListItem>
        {
            new SelectListItem { Text = "Group Training" , Value = "GRP"},
            new SelectListItem { Text = "Individual" , Value = "INDI" }
        };

        return list;
    }
    /// <summary>Get gender dropdown.</summary>
    public static List<SelectListItem> GetGenderDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Male", Value = "Male" },
            new SelectListItem { Text = "Female", Value = "Female" },
            new SelectListItem { Text = "Transgender", Value = "Transgender" },
            new SelectListItem { Text = "Other", Value = "Other" }
        };
    }

    /// <summary>Get level configuration dropdown.</summary>
    public static List<SelectListItem> GetLevelConfigurationDropdown()
    {

        var list = new List<SelectListItem>
        {
            new SelectListItem { Text = "Level One" , Value = "LEVELONE"},
            new SelectListItem { Text = "Level Two" , Value = "LEVELTWO" }
        };

        return list;
    }

    // Values mirror the helpdesk.support_tickets ck_support_tickets__status CHECK constraint.
    /// <summary>Get support status dropdown.</summary>
    public static List<SelectListItem> GetSupportStatusDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Open",              Value = "open" },
            new SelectListItem { Text = "In Progress",       Value = "in_progress" },
            new SelectListItem { Text = "Waiting Customer",  Value = "waiting_customer" },
            new SelectListItem { Text = "Resolved",          Value = "resolved" },
            new SelectListItem { Text = "Closed",            Value = "closed" },
            new SelectListItem { Text = "Reopened",          Value = "reopened" },
        };
    }

    // Values mirror the helpdesk.sos_requests ck_sos_requests__status CHECK constraint.
    /// <summary>Get SOS status dropdown.</summary>
    public static List<SelectListItem> GetSosStatusDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Raised",        Value = "raised" },
            new SelectListItem { Text = "Acknowledged",  Value = "acknowledged" },
            new SelectListItem { Text = "Dispatched",    Value = "dispatched" },
            new SelectListItem { Text = "Resolved",      Value = "resolved" },
            new SelectListItem { Text = "Cancelled",     Value = "cancelled" },
        };
    }

    // Values mirror the helpdesk.sos_requests ck_sos_requests__priority CHECK constraint.
    /// <summary>Get SOS priority dropdown.</summary>
    public static List<SelectListItem> GetSosPriorityDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Low",       Value = "low" },
            new SelectListItem { Text = "Medium",    Value = "medium" },
            new SelectListItem { Text = "High",      Value = "high" },
            new SelectListItem { Text = "Critical",  Value = "critical" },
        };
    }

    // Values must match the CHECK on monitor.exception_logs.severity
    // (see db/0021_tables_monitor.sql): info | warn | error | critical.
    /// <summary>Get exception severity dropdown.</summary>
    public static List<SelectListItem> GetExceptionSeverityDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Info",     Value = "info" },
            new SelectListItem { Text = "Warning",  Value = "warn" },
            new SelectListItem { Text = "Error",    Value = "error" },
            new SelectListItem { Text = "Critical", Value = "critical" },
        };
    }

    // Values must match the CHECK on monitor.exception_logs.source
    // (see db/0021_tables_monitor.sql): kiosk | admin | api | job | integration.
    /// <summary>Get exception source dropdown.</summary>
    public static List<SelectListItem> GetExceptionSourceDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Kiosk",       Value = "kiosk" },
            new SelectListItem { Text = "Admin",       Value = "admin" },
            new SelectListItem { Text = "API",         Value = "api" },
            new SelectListItem { Text = "Job",         Value = "job" },
            new SelectListItem { Text = "Integration", Value = "integration" },
        };
    }

    // Values must match the CHECK on audit.audit_events.log_type
    // (see db/0012_tables_audit.sql): INFO | WARN | ERROR | SECURITY.
    /// <summary>Get audit log type dropdown.</summary>
    public static List<SelectListItem> GetAuditLogTypeDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Info",     Value = "INFO" },
            new SelectListItem { Text = "Warning",  Value = "WARN" },
            new SelectListItem { Text = "Error",    Value = "ERROR" },
            new SelectListItem { Text = "Security", Value = "SECURITY" },
        };
    }

    // Values must match the CHECK on ops.kiosk_inventory_snapshots.metal
    // (see db/0020_tables_ops.sql): gold | silver | platinum | palladium.
    /// <summary>Get metal dropdown.</summary>
    public static List<SelectListItem> GetMetalDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Gold",      Value = "gold" },
            new SelectListItem { Text = "Silver",    Value = "silver" },
            new SelectListItem { Text = "Platinum",  Value = "platinum" },
            new SelectListItem { Text = "Palladium", Value = "palladium" },
        };
    }

    // Values must match the CHECK on ops.deployment_tickets.status / ops.maintenance_tickets.status
    // (see db/0020_tables_ops.sql): open | in_progress | resolved | closed | reopened | other.
    /// <summary>Get ticket status dropdown.</summary>
    public static List<SelectListItem> GetTicketStatusDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Open",        Value = "open" },
            new SelectListItem { Text = "In Progress", Value = "in_progress" },
            new SelectListItem { Text = "Resolved",    Value = "resolved" },
            new SelectListItem { Text = "Closed",      Value = "closed" },
            new SelectListItem { Text = "Reopened",    Value = "reopened" },
            new SelectListItem { Text = "Other",       Value = "other" },
        };
    }

    // Values must match the CHECK on ops.deployment_tickets.priority / ops.maintenance_tickets.priority.
    /// <summary>Get ticket priority dropdown.</summary>
    public static List<SelectListItem> GetTicketPriorityDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Low",    Value = "low" },
            new SelectListItem { Text = "Normal", Value = "normal" },
            new SelectListItem { Text = "High",   Value = "high" },
            new SelectListItem { Text = "Urgent", Value = "urgent" },
        };
    }

    // Values must match the CHECK on ops.maintenance_tickets.ticket_type
    // (see db/0020_tables_ops.sql): preventive | corrective | calibration | cleaning.
    /// <summary>Get maintenance type dropdown.</summary>
    public static List<SelectListItem> GetMaintenanceTypeDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Preventive",  Value = "preventive" },
            new SelectListItem { Text = "Corrective",  Value = "corrective" },
            new SelectListItem { Text = "Calibration", Value = "calibration" },
            new SelectListItem { Text = "Cleaning",    Value = "cleaning" },
        };
    }

    // Values must match the CHECK on ops.collection_runs.status
    // (see db/0020_tables_ops.sql): planned | in_progress | completed | cancelled.
    /// <summary>Get collection run status dropdown.</summary>
    public static List<SelectListItem> GetCollectionRunStatusDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Planned",     Value = "planned" },
            new SelectListItem { Text = "In Progress", Value = "in_progress" },
            new SelectListItem { Text = "Completed",   Value = "completed" },
            new SelectListItem { Text = "Cancelled",   Value = "cancelled" },
        };
    }

    // Values must match the CHECK on ops.collection_tickets.status
    // (see db/0020_tables_ops.sql): pending | collected | skipped | discrepancy.
    /// <summary>Get collection ticket status dropdown.</summary>
    public static List<SelectListItem> GetCollectionTicketStatusDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Pending",     Value = "pending" },
            new SelectListItem { Text = "Collected",   Value = "collected" },
            new SelectListItem { Text = "Skipped",     Value = "skipped" },
            new SelectListItem { Text = "Discrepancy", Value = "discrepancy" },
        };
    }

    // Values must match the CHECK on ops.technicians.status
    // (see db/0020_tables_ops.sql): active | on_leave | inactive.
    /// <summary>Get technician status dropdown.</summary>
    public static List<SelectListItem> GetTechnicianStatusDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "Active",   Value = "active" },
            new SelectListItem { Text = "On Leave", Value = "on_leave" },
            new SelectListItem { Text = "Inactive", Value = "inactive" },
        };
    }

    /// <summary>Get do dont type dropdown.</summary>
    public static List<SelectListItem> GetDoDontTypeDropdown()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Text = "DO", Value = "DO" },
            new SelectListItem { Text = "DON'T", Value = "DONT" }
        };
    }

    //Carbon footprints dropdowns

    /// <summary>Entry type option.</summary>
    public class EntryTypeOption
    {
        /// <summary>Gets or sets the value.</summary>
        public string Value { get; set; } = string.Empty;
        /// <summary>Gets or sets the label.</summary>
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>Machine type option.</summary>
    public class MachineTypeOption
    {
        /// <summary>Gets or sets the value.</summary>
        public string Value { get; set; } = string.Empty;
        /// <summary>Gets or sets the label.</summary>
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>Energy type option.</summary>
    public class EnergyTypeOption
    {
        /// <summary>Gets or sets the value.</summary>
        public string Value { get; set; } = string.Empty;
        /// <summary>Gets or sets the label.</summary>
        public string Label { get; set; } = string.Empty;
        /// <summary>Gets or sets the emission factor.</summary>
        public double EmissionFactor { get; set; }
    }

    /// <summary>Unit option.</summary>
    public class UnitOption
    {
        /// <summary>Gets or sets the value.</summary>
        public string? Value { get; set; }
        /// <summary>Gets or sets the label.</summary>
        public string? Label { get; set; }
    }

    /// <summary>Dropdown data.</summary>
    public static class DropdownData
    {
        /// <summary>New.</summary>
        public static List<EntryTypeOption> EntryTypes => new()
{
    new() { Value = "Product", Label = "Product" },
    new() { Value = "Factory", Label = "Factory" }
};

        /// <summary>New.</summary>
        public static List<MachineTypeOption> MachineTypes => new()
{
    new() { Value = "Compressor", Label = "Compressor" },
    new() { Value = "Conveyor", Label = "Conveyor Belt" },
    new() { Value = "CNCMachine", Label = "CNC Machine" },
    new() { Value = "Boiler", Label = "Boiler" },
    new() { Value = "Generator", Label = "Generator" },
    new() { Value = "HVAC", Label = "HVAC System" },
    new() { Value = "WeldingMachine", Label = "Welding Machine" },
    new() { Value = "PumpMotor", Label = "Pump / Motor" },
    new() { Value = "Other", Label = "Other" }
};

        /// <summary>New.</summary>
        public static List<EnergyTypeOption> EnergyTypes => new()
{
    new() { Value = "Electricity", Label = "Electricity", EmissionFactor = 0.233 },
    new() { Value = "Diesel", Label = "Diesel", EmissionFactor = 2.68 },
    new() { Value = "Gas", Label = "Natural Gas", EmissionFactor = 2.04 }
};

        /// <summary>New.</summary>
        public static List<UnitOption> Units => new()
{
    new() { Value = "KWh", Label = "KWh (Kilowatt-hour)" },
    new() { Value = "Liter", Label = "Liter" }
};

    }
}

/*public class PortalHelpersNonStatic
{
    private readonly STKDbContext _context;
    private readonly IExceptionLogger _exceptionLogger;
    private readonly string _clientCode;
    private readonly string _featureCode;
    public PortalHelpersNonStatic(STKDbContext context, IExceptionLogger exceptionLogger, string clientCode, string featureCode)
    {
        _context = context;
        _exceptionLogger = exceptionLogger;
        _clientCode = clientCode;
        _featureCode = featureCode;
    }

    public List<SelectListItem> GetModuleDropdown()
    {
        var Module = _context.ClientModuleMasters
                            .Where(i => i.IsActive && i.ClientCode == _clientCode && i.FeatureCode == _featureCode && i.LanguageCode == "en")
                            .AsEnumerable()
                            .DistinctBy(i => i.Code)
                            .Select(i => new SelectListItem
                            {
                                Text = i.Name,
                                Value = i.Code
                            })
                            .ToList();

        return Module;
    }


    public List<SelectListItem> GetChapterDropdown()
    {
        var Chapter = _context.ClientChapterMasters
                            .Where(i => i.IsActive && i.ClientCode == _clientCode && i.FeatureCode == _featureCode)
                            .AsEnumerable()
                            .DistinctBy(i => i.Code)
                            .Select(i => new SelectListItem
                            {
                                Text = i.Name,
                                Value = i.Code
                            })
                            .ToList();

        return Chapter;

    }

    public List<SelectListItem> GetLessonDropdown()
    {
        var Chapter = _context.ClientLessonMasters
                            .Where(i => i.IsActive && i.ClientCode == _clientCode && i.FeatureCode == _featureCode)
                            .AsEnumerable()
                            .DistinctBy(i => i.Code)
                            .Select(i => new SelectListItem
                            {
                                Text = i.Name,
                                Value = i.Code
                            })
                            .ToList();

        return Chapter;
    }

    public List<SelectListItem> GetEmployeeTypeDropdown()
    {
        return _context.EmployeeTypeMasters
                       .Where(i => i.IsActive && i.ClientCode == _clientCode && i.FeatureCode == _featureCode)
                       .Select(i => new SelectListItem
                       {
                           Text = i.Name,
                           Value = i.Code
                       })
                       .ToList();
    }


    public List<SelectListItem> GetLevelEmployeeDropDown(string level)
    {
        return (from lc in _context.LevelConfigurations
                join e in _context.EmployeeMasters
                    on lc.EmpId equals e.EmployeeCode
                where lc.IsActive
                      && lc.ClientCode == _clientCode
                      && lc.LevelCode == level
                select new SelectListItem
                {
                    Text = e.EmployeeName,   // Show employee name
                    Value = e.EmployeeCode   // Use employee code as value
                })
                .ToList();
    }





    public List<SelectListItem> GetEmployeeSpecificTypeDropdown()
    {
        return _context.EmployeeTypeMasters
                       .Where(i => i.IsActive && i.ClientCode == _clientCode && i.FeatureCode == _featureCode)
                       .Select(i => new SelectListItem
                       {
                           Text = i.Name,
                           Value = i.Code
                       })
                       .ToList();
    }
    public List<SelectListItem> GetTypeDropdown()
    {
        return _context.EmployeeTypeMasters
                       .Where(i => i.IsActive && i.ClientCode == _clientCode)
                       .Select(i => new SelectListItem
                       {
                           Text = i.Name,
                           Value = i.Code
                       })
                       .ToList();
    }

    public List<SelectListItem> GetSafetyTypeDropdown()
    {
        return _context.EmployeeTypeMasters
                       .Where(i => i.IsActive && i.ClientCode == _clientCode)
                       .Select(i => new SelectListItem
                       {
                           Text = i.Name,
                           Value = i.Code
                       })
                       .ToList();
    }

    public List<SelectListItem> GetEventListDropdown()
    {
        var today = DateTime.Today;
        return _context.SchedulerEvents
                       .Where(i => i.IsActive
                                   && i.ClientCode == _clientCode
                                   && i.EventDateTime >= today)
                       .Select(i => new SelectListItem
                       {
                           Text = i.EventName,
                           Value = i.Code
                       })
                       .ToList();
    }


    public List<SelectListItem> GetLanguagelistDropdown()
    {
        return _context.LanguageMasters
                       .Where(i => i.IsActive)
                       .Select(i => new SelectListItem
                       {
                           Text = i.LanguageName,
                           Value = i.LanguageCode
                       })
                       .ToList();
    }

    public List<SelectListItem> GetLanguageDropdown()
    {
        var lang = _context.ClientMasters.FirstOrDefault(i => i.Code == _clientCode);

        if (lang != null && !string.IsNullOrEmpty(lang.Languages))
        {
            // Assuming Languages is stored as a comma-separated string (e.g., "en,hi,kn")
            var languageCodes = lang.Languages.Split(',').ToList();

            //forced en always first 
            if (!languageCodes.Contains("en"))
                languageCodes.Insert(0, "en");

            var languageList = _context.LanguageMasters
                .Where(i => languageCodes.Contains(i.LanguageCode))
                .Select(i => new SelectListItem
                {
                    Text = i.LanguageName,
                    Value = i.LanguageCode
                })
                .ToList();

            return languageList.OrderBy(i => i.Value == "en" ? 0 : 1).ToList();
        }
        return new List<SelectListItem>();
    }

    public List<SelectListItem> GetAllLanguageDropdown()
    {
        var lang = _context.LanguageMasters
            .Where(i => i.IsActive == true)
            .Select(i => new SelectListItem
            {
                Text = i.LanguageName,
                Value = i.LanguageCode
            })
                .ToList();

        return lang;
    }
    public List<SelectListItem> GetAllCategoryDropdown()
    {
        var lang = _context.CategoryMasters
            .Where(i => i.IsActive == true)
            .Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Code
            })
                .ToList();

        return lang;
    }

    public List<languageViewModel> GetLanguageList()
    {
        var lang = _context.ClientMasters.FirstOrDefault(i => i.Code == _clientCode);

        if (lang != null && !string.IsNullOrEmpty(lang.Languages))
        {
            // Assuming Languages is stored as a comma-separated string (e.g., "en,hi,kn")
            var languageCodes = lang.Languages.Split(',').ToList();

            var languageList = _context.LanguageMasters
                .Where(i => languageCodes.Contains(i.LanguageCode))
                .Select(i => new languageViewModel
                {
                    LanguageCode = i.LanguageCode,
                    LanguageName = i.LanguageName
                })
                .ToList();

            return languageList;
        }

        return new List<languageViewModel>();
    }
    public List<SelectListItem> GetFeatureDropdown()
    {
        var feat = _context.ClientMasters.FirstOrDefault(i => i.Code == _clientCode);

        if (feat != null && !string.IsNullOrEmpty(feat.Features))
        {
            var featuresCodes = feat.Features.Split(',').ToList();

            var featureList = _context.FeaturesMasters
                .Where(i => featuresCodes.Contains(i.Code) && i.ClientCode == _clientCode)
                .Select(i => new SelectListItem
                {
                    Text = i.FeatureName,
                    Value = i.Code
                })
                .ToList();

            return featureList;
        }
        return new List<SelectListItem>();
    }
    public List<SelectListItem> GetSpecificFeatureDropdown()
    {
        var client = _context.ClientMasters
            .FirstOrDefault(i => i.Code == _clientCode);

        if (client?.Features == null)
            return new List<SelectListItem>();

        var featureCodes = client.Features
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToList();

        var excludedCodes = new[] { "GRV", "FEEDB", "WIB", "HRMS", "GROUP", "SR", "MAP", "AUDIT" };

        var featureList = _context.FeaturesMasters
            .Where(i =>
                featureCodes.Contains(i.Code) &&
                i.ClientCode == _clientCode &&
                !excludedCodes.Contains(i.Code))
            .Select(i => new SelectListItem
            {
                Text = i.FeatureName,
                Value = i.Code
            })
            .ToList();

        return featureList;
    }
    public List<SelectListItem> GetSpecificFeature2Dropdown()
    {
        var client = _context.ClientMasters
            .FirstOrDefault(i => i.Code == _clientCode);

        if (client?.Features == null)
            return new List<SelectListItem>();

        var featureCodes = client.Features
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToList();

        var excludedCodes = new[] { "SFTY", "VIM", "VEM", "HRMS", "GROUP", "CONT" };

        var featureList = _context.FeaturesMasters
            .Where(i =>
                featureCodes.Contains(i.Code) &&
                i.ClientCode == _clientCode &&
                !excludedCodes.Contains(i.Code))
            .Select(i => new SelectListItem
            {
                Text = i.FeatureName,
                Value = i.Code
            })
            .ToList();

        return featureList;
    }
    public List<SelectListItem> GetSpecificFeature3Dropdown()
    {
        var client = _context.ClientMasters
            .FirstOrDefault(i => i.Code == _clientCode);

        if (client?.Features == null)
            return new List<SelectListItem>();

        var featureCodes = client.Features
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToList();

        var excludedCodes = new[] { "SFTY", "VIM", "VEM", "GRV", "FEEDB", "WIB", "HRMS", "GROUP" };

        var featureList = _context.FeaturesMasters
            .Where(i =>
                featureCodes.Contains(i.Code) &&
                i.ClientCode == _clientCode &&
                !excludedCodes.Contains(i.Code))
            .Select(i => new SelectListItem
            {
                Text = i.FeatureName,
                Value = i.Code
            })
            .ToList();

        return featureList;
    }
    public List<SelectListItem> GetSpecificFeature4Dropdown()
    {
        var client = _context.ClientMasters
            .FirstOrDefault(i => i.Code == _clientCode);

        if (client?.Features == null)
            return new List<SelectListItem>();

        var featureCodes = client.Features
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToList();

        var excludedCodes = new[] { "GRV", "FEEDB", "WIB", "HRMS", "GROUP" };

        var featureList = _context.FeaturesMasters
            .Where(i =>
                featureCodes.Contains(i.Code) &&
                i.ClientCode == _clientCode &&
                !excludedCodes.Contains(i.Code))
            .Select(i => new SelectListItem
            {
                Text = i.FeatureName,
                Value = i.Code
            })
            .ToList();

        return featureList;
    }
    public List<SelectListItem> GetRoleDropdown()
    {
        var role = _context.RoleMasters
                .Where(i => i.ClientCode == _clientCode)
                .Select(i => new SelectListItem
                {
                    Text = i.RoleName,
                    Value = i.Code
                })
                .ToList();

        return role;

    }
    public List<SelectListItem> GetDesignationDropdown()
    {
        var role = _context.Designations
                .Where(i => i.ClientCode == _clientCode)
                .Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Code
                })
                .ToList();

        return role;

    }
    public List<ClientModuleMaster> GetModule()
    {
        var Module = _context.ClientModuleMasters
                            .Where(i => i.ClientCode == _clientCode && i.FeatureCode == _featureCode && i.LanguageCode == "en")
                            .AsEnumerable()
                            .DistinctBy(i => i.Code)
                            .ToList();

        return Module;
    }
    public List<ClientChapterMaster> GetChapterByModule(string Module)
    {
        var Chapter = _context.ClientChapterMasters
                            .Where(i => i.ClientCode == _clientCode && i.FeatureCode == _featureCode && i.ModuleCode == Module && i.LanguageCode == "en")
                            .AsEnumerable()
                            .DistinctBy(i => i.Code)

                            .ToList();

        return Chapter;

    }
    public List<ClientLessonMaster> GetLessonByChapter(string Chapter)
    {
        var Lesson = _context.ClientLessonMasters
                            .Where(i => i.ClientCode == _clientCode && i.FeatureCode == _featureCode && i.ChapterCode == Chapter && i.LanguageCode == "en")
                            .AsEnumerable()
                            .DistinctBy(i => i.Code)

                            .ToList();
        return Lesson;

    }
    public List<SelectListItem> GetEmployeeDropdown()
    {
        var emp = _context.EmployeeMasters
                .Where(i => i.ClientCode == _clientCode)
                .Select(i => new SelectListItem
                {
                    Text = i.EmployeeName,
                    Value = i.EmployeeCode
                })
                .ToList();

        return emp;

    }
    public List<SelectListItem> GetUrlTypeDropdown()
    {
        return _context.EmployeeTypeMasters
                       .Where(i => i.IsActive && i.ClientCode == _clientCode && i.FeatureCode == _featureCode)
                       .Select(i => new SelectListItem
                       {
                           Text = i.Name,
                           Value = i.Code
                       })
                       .ToList();
    }

    public List<SelectListItem> GetFeedBackCategoryDropdown()
    {
        var cate = _context.ClientCategoryMasters
            .Where(i => i.IsActive == true && i.ClientCode == _clientCode && i.FeatureCode == "FEEDB")
            .Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Name
            })
                .ToList();

        return cate;
    }
    public List<SelectListItem> GetIncidentCategoryDropdown()
    {
        var cate = _context.ClientCategoryMasters
            .Where(i => i.IsActive == true && i.ClientCode == _clientCode && i.FeatureCode == "GRV")
            .Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Name
            })
                .ToList();

        return cate;
    }
    public List<SelectListItem> GetWhistleBlowerCategoryDropdown()
    {
        var cate = _context.ClientCategoryMasters
            .Where(i => i.IsActive == true && i.ClientCode == _clientCode && i.FeatureCode == "WIB")
            .Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Name
            })
                .ToList();

        return cate;
    }

    public List<SelectListItem> GetLocationDropdown()
    {
        return _context.ClientMasters
                       .Where(i => i.IsActive && i.organizationID == _clientCode)
                       .Select(i => new SelectListItem
                       {
                           Text = i.Name,
                           Value = i.Code
                       })
                       .ToList();
    }

    public List<SelectListItem> GetContractorDropdown()
    {
        var Chapter = _context.ContractorMasters
                            .Where(i => i.IsActive && i.ClientCode == _clientCode)
                            .AsEnumerable()
                            .DistinctBy(i => i.ContractorCode)
                            .Select(i => new SelectListItem
                            {
                                Text = i.ContractorName,
                                Value = i.ContractorName
                            })
                            .ToList();

        return Chapter;
    }
    public List<SelectListItem> GetPlantDropdown()
    {
        var Chapter = _context.Plants
                            .Where(i => i.IsActive && i.ClientCode == _clientCode)
                            .AsEnumerable()
                            .DistinctBy(i => i.Code)
                            .Select(i => new SelectListItem
                            {
                                Text = i.Name,
                                Value = i.Code
                            })
                            .ToList();

        return Chapter;
    }
    public List<SelectListItem> GetDepartmentDropdown()
    {
        var Chapter = _context.Departments
                            .Where(i => i.IsActive && i.ClientCode == _clientCode)
                            .AsEnumerable()
                            .DistinctBy(i => i.Code)
                            .Select(i => new SelectListItem
                            {
                                Text = i.Name,
                                Value = i.Code
                            })
                            .ToList();

        return Chapter;
    }

    public List<SelectListItem> GetDivisionDropdown()
    {
        var Chapter = _context.Divisions
                            .Where(i => i.IsActive && i.ClientCode == _clientCode)
                            .AsEnumerable()
                            .DistinctBy(i => i.Code)
                            .Select(i => new SelectListItem
                            {
                                Text = i.Name,
                                Value = i.Code
                            })
                            .ToList();

        return Chapter;
    }
    public List<SelectListItem> GetSupportCategoryDropdown()
    {
        var categories = _context.SupportCategories
                            .Where(i => i.IsActive)
                            .AsEnumerable()
                            .DistinctBy(i => i.Code)
                            .Select(i => new SelectListItem
                            {
                                Text = i.Name,
                                Value = i.Code
                            })
                            .ToList();

        return categories;
    }
    public List<SelectListItem> GetSupportSubCategoryDropdown()
    {
        var subCategories = _context.SupportSubCategories
                           .Where(i => i.IsActive)
                           .AsEnumerable()
                           .DistinctBy(i => i.Code)
                           .Select(i => new SelectListItem
                           {
                               Text = i.Name,
                               Value = i.Code
                           })
                           .ToList();

        return subCategories;
    }

    //Common Module, Lesson, Chapter
    public List<SelectListItem> GetModulesDropdown()
    {
        var Module = _context.ModuleMasters
                            .Where(i => i.IsActive && i.ClientCode == _clientCode)
                            .AsEnumerable()
                            .DistinctBy(i => i.Code)
                            .Select(i => new SelectListItem
                            {
                                Text = i.Name,
                                Value = i.Code
                            })
                            .ToList();

        return Module;
    }


    public List<SelectListItem> GetChaptersDropdown()
    {
        var Chapter = _context.ChapterMasters
                            .Where(i => i.IsActive && i.ClientCode == _clientCode)
                            .AsEnumerable()
                            .DistinctBy(i => i.Code)
                            .Select(i => new SelectListItem
                            {
                                Text = i.Name,
                                Value = i.Code
                            })
                            .ToList();

        return Chapter;

    }

    public List<SelectListItem> GetLessonsDropdown()
    {
        var lesson = _context.LessonMasters
                            .Where(i => i.IsActive && i.ClientCode == _clientCode)
                            .AsEnumerable()
                            .DistinctBy(i => i.Code)
                            .Select(i => new SelectListItem
                            {
                                Text = i.Name,
                                Value = i.Code
                            })
                            .ToList();

        return lesson;
    }


    //Resource Module
    public List<SelectListItem> GetResourceModuleDropdown()
    {
        var Module = _context.ResourceMasters
                            .Where(i => i.IsActive && i.ClientCode == _clientCode)
                            .AsEnumerable()
                            .DistinctBy(i => i.ModuleCode)
                            .Select(i => new SelectListItem
                            {
                                Text = i.ModuleCode,
                                Value = i.ModuleCode
                            })
                            .ToList();

        return Module;
    }
}*/
