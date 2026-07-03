using Microsoft.AspNetCore.Mvc.Rendering;

namespace GoldKiosk.Cloud.AdminPortal.Models;

/// <summary>Viewmodel.</summary>
public class Viewmodel
{
    /// <summary>Gets or sets the request id.</summary>
    public string? RequestId { get; set; }

    /// <summary>Is null or empty.</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}


/// <summary>User role map view model.</summary>
public class UserRoleMapVM
{
    /// <summary>Gets or sets the role code.</summary>
    public string RoleCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the role name.</summary>
    public string RoleName { get; set; } = string.Empty;

    // User context
    /// <summary>Gets or sets the user id.</summary>
    public Guid UserId { get; set; }
    /// <summary>Gets or sets the user code.</summary>
    public string UserCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the email.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Gets or sets the first name.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>Gets or sets the last name.</summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>Gets or sets the mobile.</summary>
    public string Mobile { get; set; } = string.Empty;
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets the created on.</summary>
    public DateTime CreatedOn { get; set; }

    // Roles assigned to the user (used by the ProfileRoles "Add Role" modal).
    /// <summary>Gets or sets the assigned roles.</summary>
    public List<UserRoleMapVM> AssignedRoles { get; set; } = new();

    // Page-level collections
    /// <summary>Gets or sets the users.</summary>
    public List<UserRoleMapVM> Users { get; set; } = new();
    /// <summary>Gets or sets the role options.</summary>
    public List<UserRoleMapVM> RoleOptions { get; set; } = new();

    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the active.</summary>
    public int Active { get; set; }
    /// <summary>Gets or sets the in active.</summary>
    public int InActive { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }

    // Legacy alias retained to avoid breaking older views that may still read .List
    /// <summary>Gets the list.</summary>
    public List<UserRoleMapVM> List
    {
        get => Users;
        set => Users = value;
    }
}

/// <summary>API payload.</summary>
public class ApiPayload
{
    /// <summary>Gets or sets a value indicating whether status.</summary>
    public bool Status { get; set; } = true;
    /// <summary>Gets or sets the message.</summary>
    public string? Message { get; set; } = string.Empty;

}


/// <summary>Admin login view model.</summary>
public class AdminLoginViewModel : ApiPayload
{
    /// <summary>Gets or sets the email.</summary>
    public string? Email { get; set; } = string.Empty;
    /// <summary>Gets or sets the password.</summary>
    public string? Password { get; set; } = string.Empty;
    /// <summary>Gets or sets the user mobile.</summary>
    public string? UserMobile { get; set; } = string.Empty;
    /// <summary>Gets or sets the first name.</summary>
    public string? FirstName { get; set; } = string.Empty;
    /// <summary>Gets or sets the last name.</summary>
    public string? LastName { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether remember me.</summary>
    public bool RememberMe { get; set; }
    /// <summary>Gets or sets the OTP mode.</summary>
    public string? OtpMode { get; set; } = string.Empty;
    /// <summary>Gets or sets the OTP.</summary>
    public string? Otp { get; set; } = string.Empty;
    /// <summary>Gets or sets the mobile.</summary>
    public string? Mobile { get; set; } = string.Empty;
    /// <summary>Gets or sets the confirm password.</summary>
    public string? ConfirmPassword { get; set; } = string.Empty;
    /// <summary>Gets or sets the user code.</summary>
    public string? UserCode { get; set; } = string.Empty;
}


/// <summary>Access control page view model.</summary>
public class AccessControlPageViewModel
{
    /// <summary>Gets or sets the items.</summary>
    public List<AccessModuleViewModel> Items { get; set; } = new();
    /// <summary>Gets or sets the role code.</summary>
    public string RoleCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the search.</summary>
    public string Search { get; set; } = string.Empty;
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; } = 1;
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; } = 10;
    /// <summary>Gets or sets the total records.</summary>
    public int TotalRecords { get; set; }

    /// <summary>Ceiling.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalRecords / PageSize);
}


/// <summary>Access module view model.</summary>
public class AccessModuleViewModel
{
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the controller name.</summary>
    public string ControllerName { get; set; } = string.Empty;
    /// <summary>Gets or sets the action name.</summary>
    public string ActionName { get; set; } = string.Empty;
    /// <summary>Gets or sets the module name.</summary>
    public string ModuleName { get; set; } = string.Empty;
    /// <summary>Gets or sets the sub module name.</summary>
    public string SubModuleName { get; set; } = string.Empty;
    /// <summary>Gets or sets the sub sub module name.</summary>
    public string SubSubModuleName { get; set; } = string.Empty;
    /// <summary>Gets or sets the type.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Gets or sets the created on.</summary>
    public DateTime? CreatedOn { get; set; }
    /// <summary>Gets or sets the updated on.</summary>
    public DateTime? UpdatedOn { get; set; }
    /// <summary>Gets or sets the created by.</summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the updated by.</summary>
    public string UpdatedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets the available types.</summary>
    public HashSet<string> AvailableTypes { get; set; } = [];
    /// <summary>Gets or sets a value indicating whether is view.</summary>
    public bool IsView { get; set; }
    /// <summary>Gets or sets a value indicating whether is add.</summary>
    public bool IsAdd { get; set; }
    /// <summary>Gets or sets a value indicating whether is edit.</summary>
    public bool IsEdit { get; set; }
    /// <summary>Gets or sets a value indicating whether is delete.</summary>
    public bool IsDelete { get; set; }
    /// <summary>Gets or sets a value indicating whether is other.</summary>
    public bool IsOther { get; set; }
    /// <summary>Gets or sets the role code.</summary>
    public string RoleCode { get; set; } = string.Empty;
}


/// <summary>Role master view model.</summary>
public class RoleMasterViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string? Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the name.</summary>
    public string? Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the client code.</summary>
    public string? ClientCode { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets the created on.</summary>
    public DateTime CreatedOn { get; set; } = DateTime.Now;
}
/// <summary>Role master list.</summary>
public class RoleMasterList
{
    /// <summary>Gets or sets the role master list.</summary>
    public List<RoleMasterViewModel> roleMasterList { get; set; } = new List<RoleMasterViewModel>();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the active.</summary>
    public int Active { get; set; }
    /// <summary>Gets or sets the in active.</summary>
    public int InActive { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
}


/// <summary>Kiosk master view model.</summary>
public class KioskMasterViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the kiosk code.</summary>
    public string? KioskCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the kiosk name.</summary>
    public string? KioskName { get; set; } = string.Empty;
    /// <summary>Gets or sets the client code.</summary>
    public string? ClientCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the kiosk PIN.</summary>
    public string? KioskPin { get; set; } = string.Empty;
    /// <summary>Gets or sets the kiosk status.</summary>
    public string? KioskStatus { get; set; } = string.Empty;
    /// <summary>Gets or sets the address.</summary>
    public string? Address { get; set; } = string.Empty;
    /// <summary>Gets or sets the device id.</summary>
    public string? DeviceId { get; set; } = string.Empty;
    /// <summary>Gets or sets the network speed.</summary>
    public string? NetworkSpeed { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is maintenance.</summary>
    public bool IsMaintenance { get; set; }
    /// <summary>Gets or sets a value indicating whether is enable.</summary>
    public bool IsEnable { get; set; }
    /// <summary>Gets or sets a value indicating whether is disable.</summary>
    public bool IsDisable { get; set; }
    /// <summary>Gets or sets a value indicating whether is reboot.</summary>
    public bool IsReboot { get; set; }
    /// <summary>Gets or sets the last ping.</summary>
    public DateTime LastPing { get; set; } = DateTime.Now;
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets the created on.</summary>
    public DateTime CreatedOn { get; set; } = DateTime.Now;
    /// <summary>Gets or sets the updated on.</summary>
    public DateTime UpdatedOn { get; set; } = DateTime.Now;
    /// <summary>Gets or sets the onboarding date.</summary>
    public DateTime OnboardingDate { get; set; } = DateTime.Now;
}
/// <summary>Kiosk master list.</summary>
public class KioskMasterList
{
    /// <summary>Gets or sets the kiosk master list.</summary>
    public List<KioskMasterViewModel> kioskMasterList { get; set; } = new List<KioskMasterViewModel>();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the active.</summary>
    public int Active { get; set; }
    /// <summary>Gets or sets the in active.</summary>
    public int InActive { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
}
/// <summary>Screen saver master view model.</summary>
public class ScreenSaverMasterViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string? Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the img URL.</summary>
    public string? ImgUrl { get; set; } = string.Empty;
    /// <summary>Gets or sets the client code.</summary>
    public string? ClientCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the order no.</summary>
    public string? OrderNo { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets the created on.</summary>
    public DateTime CreatedOn { get; set; } = DateTime.Now;
}
/// <summary>Screen saver master list.</summary>
public class ScreenSaverMasterList
{
    /// <summary>Gets or sets the screen saver master list.</summary>
    public List<ScreenSaverMasterViewModel> screenSaverMasterList { get; set; } = new List<ScreenSaverMasterViewModel>();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the active.</summary>
    public int Active { get; set; }
    /// <summary>Gets or sets the in active.</summary>
    public int InActive { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
}

/// <summary>Client view model.</summary>
public class ClientVm
{

    /// <summary>Gets or sets the all clients.</summary>
    public int AllClients { get; set; }

    /// <summary>Gets or sets the registered clients.</summary>
    public int RegisteredClients { get; set; }

    /// <summary>Gets or sets the process clients.</summary>
    public int ProcessClients { get; set; }

    /// <summary>Gets or sets the unpaid clients.</summary>
    public int UnpaidClients { get; set; }

    /// <summary>Gets or sets the closed clients.</summary>
    public int ClosedClients { get; set; }

    /// <summary>Gets or sets the source dropdown.</summary>
    public List<SelectListItem>? SourceDropdown { get; set; }

    /// <summary>Gets or sets the coordinator dropdown.</summary>
    public List<SelectListItem>? CoordinatorDropdown { get; set; }

    /*public List<SelectListItem>? CategoryDropdown { get; set; }

    public List<SelectListItem>? CountryDropdown { get; set; }

    public List<SelectListItem>? BranchDropdown { get; set; }*/

}


/// <summary>Feedback list view model.</summary>
public class FeedbackListViewModel
{
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the function name.</summary>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>Gets or sets the type.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the audio URL.</summary>
    public string? AudioUrl { get; set; } = string.Empty;
    /// <summary>Gets or sets the created on.</summary>
    public DateTime CreatedOn { get; set; } = DateTime.Now;
    /// <summary>Gets or sets a value indicating whether isread.</summary>
    public bool Isread { get; set; }
    /// <summary>Gets or sets the img URL.</summary>
    public string? ImgUrl { get; set; } = string.Empty;
    /// <summary>Gets or sets the location.</summary>
    public string? Location { get; set; } = string.Empty;
}
/// <summary>Feedback master view model.</summary>
public class FeedbackMasterVM
{
    /// <summary>Gets or sets the feedback list.</summary>
    public List<FeedbackListViewModel> FeedbackItems { get; set; } = new List<FeedbackListViewModel>();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the read.</summary>
    public int Read { get; set; }
    /// <summary>Gets or sets the unread.</summary>
    public int Unread { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
}


/// <summary>Support ticket master list.</summary>
public class SupportTicketMasterList
{
    /// <summary>Gets or sets the support tickets list.</summary>
    public List<SupportTicketViewModel> SupportTicketItems { get; set; } = new List<SupportTicketViewModel>();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the active.</summary>
    public int Active { get; set; }
    /// <summary>Gets or sets the in active.</summary>
    public int InActive { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
}

/// <summary>Support ticket view model.</summary>
public class SupportTicketViewModel
{
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the category code.</summary>
    public string CategoryCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the category name.</summary>
    public string CategoryName { get; set; } = string.Empty;
    /// <summary>Gets or sets the sub category code.</summary>
    public string SubCategoryCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the sub category name.</summary>
    public string SubCategoryName { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the remarks.</summary>
    public string Remarks { get; set; } = string.Empty;
    /// <summary>Gets or sets the documents.</summary>
    public string Documents { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Gets or sets a value indicating whether is closed.</summary>
    public bool IsClosed { get; set; }
    /// <summary>Gets or sets a value indicating whether is reopened.</summary>
    public bool IsReopened { get; set; }
    /// <summary>Gets or sets the created by.</summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the updated by.</summary>
    public string UpdatedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the closed by.</summary>
    public string ClosedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the reopened by.</summary>
    public string ReopenedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the client code.</summary>
    public string ClientCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the created on.</summary>
    public DateTime CreatedOn { get; set; } = DateTime.Now;
    /// <summary>Gets or sets the updated on.</summary>
    public DateTime UpdatedOn { get; set; } = DateTime.Now;
    /// <summary>Gets or sets the closed on.</summary>
    public DateTime? ClosedOn { get; set; }
    /// <summary>Gets or sets the reopened on.</summary>
    public DateTime? ReopenedOn { get; set; }
}


/// <summary>User activity list.</summary>
public class UserActivityList
{
    /// <summary>Gets or sets the activity list.</summary>
    public List<UserActivity> ActivityList { get; set; } = new List<UserActivity>();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}


/// <summary>User activity.</summary>
public class UserActivity
{


    /// <summary>Gets or sets the log type.</summary>
    public string LogType { get; set; } = string.Empty;

    /// <summary>Gets or sets the activity.</summary>
    public string Activity { get; set; } = string.Empty;

    /// <summary>Gets or sets the module.</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>Gets or sets the sub module.</summary>
    public string SubModule { get; set; } = string.Empty;

    /// <summary>Gets or sets the sub sub module.</summary>
    public string SubSubModule { get; set; } = string.Empty;

    /// <summary>Gets or sets the updated by.</summary>
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the updated on.</summary>
    public DateTime UpdatedOn { get; set; } = DateTime.Now;

}

// ─── Customer management ────────────────────────────────────────────────
/// <summary>Customer master view model.</summary>
public class CustomerMasterViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the customer code.</summary>
    public string CustomerCode { get; set; } = string.Empty;
    /// <summary>Mobile number is stored encrypted; only a redacted placeholder is shown in lists.</summary>
    public string MobileMasked { get; set; } = "***";
    /// <summary>Cleartext mobile, accepted from Add/Edit form input only.</summary>
    public string? Mobile { get; set; }
    /// <summary>Cleartext wallet PIN, accepted from Add/Edit form input only.</summary>
    public string? WalletPin { get; set; }
    /// <summary>Gets or sets the running wallet balance.</summary>
    public decimal RunningWalletBalance { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "prospect";
    /// <summary>Gets or sets the created on.</summary>
    public DateTime CreatedOn { get; set; }
}

/// <summary>Customer master list.</summary>
public class CustomerMasterList
{
    /// <summary>Gets or sets the customer master list.</summary>
    public List<CustomerMasterViewModel> customerMasterList { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the active.</summary>
    public int Active { get; set; }
    /// <summary>Gets or sets the in active.</summary>
    public int InActive { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}

// ─── Transaction history ────────────────────────────────────────────────
/// <summary>Transaction history view model.</summary>
public class TransactionHistoryViewModel
{
    /// <summary>SR/serial code from <c>tx.transactions.transaction_code</c>.</summary>
    public string Sr { get; set; } = string.Empty;
    /// <summary>Gets or sets the mobile masked.</summary>
    public string MobileMasked { get; set; } = "***";
    /// <summary>Gets or sets the transaction id.</summary>
    public Guid TransactionId { get; set; }
    /// <summary>Gets or sets the transaction code.</summary>
    public string TransactionCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the amount.</summary>
    public decimal Amount { get; set; }
    /// <summary>Reserved — kiosk transactions don't currently carry commission; surfaced for parity with the legacy view.</summary>
    public decimal Commission { get; set; }
    /// <summary>Gets or sets the service fee.</summary>
    public decimal ServiceFee { get; set; }
    /// <summary>Gets or sets the original status.</summary>
    public string OriginalStatus { get; set; } = string.Empty;
    /// <summary>Gets or sets the final status.</summary>
    public string FinalStatus { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is reversed.</summary>
    public bool IsReversed { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the occurred on.</summary>
    public DateTime OccurredOn { get; set; }
}

/// <summary>Transaction history list.</summary>
public class TransactionHistoryList
{
    /// <summary>Gets or sets the transaction list.</summary>
    public List<TransactionHistoryViewModel> transactionList { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the success.</summary>
    public int Success { get; set; }
    /// <summary>Gets or sets the reversed.</summary>
    public int Reversed { get; set; }
    /// <summary>Gets or sets the failure.</summary>
    public int Failure { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
    /// <summary>Gets or sets the customer id.</summary>
    public Guid? CustomerId { get; set; }
    /// <summary>Gets or sets the customer code.</summary>
    public string? CustomerCode { get; set; }
}

// ────────────────────────────────────────────────────────────────────────
// Merchant management
// ────────────────────────────────────────────────────────────────────────

/// <summary>Merchant view model.</summary>
public class MerchantViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string? Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the name.</summary>
    public string? Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the email.</summary>
    public string? Email { get; set; } = string.Empty;
    /// <summary>Gets or sets the mobile.</summary>
    public string? Mobile { get; set; } = string.Empty;
    /// <summary>Gets or sets the address.</summary>
    public string? Address { get; set; } = string.Empty;
    /// <summary>Gets or sets the KYC status.</summary>
    public string? KycStatus { get; set; } = "pending";
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Gets or sets the created on.</summary>
    public DateTime CreatedOn { get; set; } = DateTime.Now;
}

/// <summary>Merchant list.</summary>
public class MerchantList
{
    /// <summary>Gets or sets the items.</summary>
    public List<MerchantViewModel> Items { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the approved.</summary>
    public int Approved { get; set; }
    /// <summary>Gets or sets the pending.</summary>
    public int Pending { get; set; }
    /// <summary>Gets or sets the rejected.</summary>
    public int Rejected { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}

/// <summary>Franchise wallet view model.</summary>
public class FranchiseWalletViewModel
{
    /// <summary>Gets or sets the wallet id.</summary>
    public Guid WalletId { get; set; }
    /// <summary>Gets or sets the merchant id.</summary>
    public Guid MerchantId { get; set; }
    /// <summary>Gets or sets the merchant code.</summary>
    public string MerchantCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the merchant name.</summary>
    public string MerchantName { get; set; } = string.Empty;
    /// <summary>Gets or sets the balance.</summary>
    public decimal Balance { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the last topup on.</summary>
    public DateTime? LastTopupOn { get; set; }
}

/// <summary>Wallet topup request view model.</summary>
public class WalletTopupRequestViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the wallet id.</summary>
    public Guid WalletId { get; set; }
    /// <summary>Gets or sets the merchant name.</summary>
    public string MerchantName { get; set; } = string.Empty;
    /// <summary>Gets or sets the merchant code.</summary>
    public string MerchantCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the requested amount.</summary>
    public decimal RequestedAmount { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "pending";
    /// <summary>Gets or sets the payment reference.</summary>
    public string? PaymentReference { get; set; }
    /// <summary>Gets or sets the rejected reason.</summary>
    public string? RejectedReason { get; set; }
    /// <summary>Gets or sets the requested on.</summary>
    public DateTime RequestedOn { get; set; }
    /// <summary>Gets or sets the approved on.</summary>
    public DateTime? ApprovedOn { get; set; }
}

/// <summary>Franchise wallets list.</summary>
public class FranchiseWalletsList
{
    /// <summary>Gets or sets the wallets.</summary>
    public List<FranchiseWalletViewModel> Wallets { get; set; } = new();
    /// <summary>Gets or sets the approved.</summary>
    public List<WalletTopupRequestViewModel> Approved { get; set; } = new();
    /// <summary>Gets or sets the pending.</summary>
    public List<WalletTopupRequestViewModel> Pending { get; set; } = new();
    /// <summary>Gets or sets the rejected.</summary>
    public List<WalletTopupRequestViewModel> Rejected { get; set; } = new();
    /// <summary>Gets or sets the all.</summary>
    public List<WalletTopupRequestViewModel> All { get; set; } = new();

    /// <summary>Gets the total requests.</summary>
    public int TotalRequests => All.Count;
    /// <summary>Gets the approved count.</summary>
    public int ApprovedCount => Approved.Count;
    /// <summary>Gets the pending count.</summary>
    public int PendingCount => Pending.Count;
    /// <summary>Gets the rejected count.</summary>
    public int RejectedCount => Rejected.Count;

    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
}

// ────────────────────────────────────────────────────────────────────────
// Voucher management
// ────────────────────────────────────────────────────────────────────────

/// <summary>Voucher view model.</summary>
public class VoucherViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string? Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the name.</summary>
    public string? Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the discount type.</summary>
    public string? DiscountType { get; set; } = "percent";
    /// <summary>Gets or sets the discount value.</summary>
    public decimal DiscountValue { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string? CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the validity start.</summary>
    public DateTime ValidityStart { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the validity end.</summary>
    public DateTime ValidityEnd { get; set; } = DateTime.UtcNow.AddMonths(1);
    /// <summary>Gets or sets the usage limit.</summary>
    public int? UsageLimit { get; set; }
    /// <summary>Gets or sets the used count.</summary>
    public int UsedCount { get; set; }
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Gets or sets the created on.</summary>
    public DateTime CreatedOn { get; set; } = DateTime.Now;
}

/// <summary>Voucher list.</summary>
public class VoucherList
{
    /// <summary>Gets or sets the items.</summary>
    public List<VoucherViewModel> Items { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the active.</summary>
    public int Active { get; set; }
    /// <summary>Gets or sets the in active.</summary>
    public int InActive { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}

/// <summary>Redemption policy view model.</summary>
public class RedemptionPolicyViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the voucher id.</summary>
    public Guid VoucherId { get; set; }
    /// <summary>Gets or sets the voucher code.</summary>
    public string VoucherCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the voucher name.</summary>
    public string VoucherName { get; set; } = string.Empty;
    /// <summary>Gets or sets the min purchase amount.</summary>
    public decimal? MinPurchaseAmount { get; set; }
    /// <summary>Gets or sets the max discount cap.</summary>
    public decimal? MaxDiscountCap { get; set; }
    /// <summary>Gets or sets the per customer limit.</summary>
    public int? PerCustomerLimit { get; set; }
    /// <summary>Gets or sets the applicable categories.</summary>
    public string? ApplicableCategories { get; set; }
    /// <summary>Gets or sets the effective from.</summary>
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the effective to.</summary>
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>Redemption policy list.</summary>
public class RedemptionPolicyList
{
    /// <summary>Gets or sets the items.</summary>
    public List<RedemptionPolicyViewModel> Items { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the active.</summary>
    public int Active { get; set; }
    /// <summary>Gets or sets the expired.</summary>
    public int Expired { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}

/// <summary>Voucher redemption view model.</summary>
public class VoucherRedemptionViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the voucher code.</summary>
    public string VoucherCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the voucher name.</summary>
    public string VoucherName { get; set; } = string.Empty;
    /// <summary>Gets or sets the customer id.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the transaction id.</summary>
    public Guid TransactionId { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }
    /// <summary>Gets or sets the redeemed amount.</summary>
    public decimal RedeemedAmount { get; set; }
    /// <summary>Gets or sets the redeemed on.</summary>
    public DateTime RedeemedOn { get; set; }
}

/// <summary>Voucher redemption list.</summary>
public class VoucherRedemptionList
{
    /// <summary>Gets or sets the items.</summary>
    public List<VoucherRedemptionViewModel> Items { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}

/// <summary>SOS request view model.</summary>
public class SosRequestViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid? KioskId { get; set; }
    /// <summary>Gets or sets the customer id.</summary>
    public Guid? CustomerId { get; set; }
    /// <summary>Gets or sets the priority.</summary>
    public string Priority { get; set; } = "high";
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "raised";
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the assigned technician id.</summary>
    public Guid? AssignedTechnicianId { get; set; }
    /// <summary>Gets or sets the raised at.</summary>
    public DateTime RaisedAt { get; set; }
    /// <summary>Gets or sets the acknowledged at.</summary>
    public DateTime? AcknowledgedAt { get; set; }
    /// <summary>Gets or sets the resolved at.</summary>
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>SOS request list.</summary>
public class SosRequestList
{
    /// <summary>Gets or sets the items.</summary>
    public List<SosRequestViewModel> Items { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the open.</summary>
    public int Open { get; set; }
    /// <summary>Gets or sets the resolved.</summary>
    public int Resolved { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}

// ─── Monitoring ─────────────────────────────────────────────────────────

/// <summary>Exception log view model.</summary>
public class ExceptionLogViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the exception name.</summary>
    public string ExceptionName { get; set; } = string.Empty;
    /// <summary>Gets or sets the message.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Gets or sets the source.</summary>
    public string Source { get; set; } = string.Empty;       // kiosk | admin | api | job | integration
    /// <summary>Gets or sets the severity.</summary>
    public string Severity { get; set; } = string.Empty;     // info | warn | error | critical
    /// <summary>Gets or sets the occurred at.</summary>
    public DateTime OccurredAt { get; set; }
    /// <summary>Gets or sets a value indicating whether is resolved.</summary>
    public bool IsResolved { get; set; }
    /// <summary>Gets or sets the resolved at.</summary>
    public DateTime? ResolvedAt { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid? TenantId { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid? KioskId { get; set; }
    /// <summary>Gets or sets the correlation id.</summary>
    public Guid? CorrelationId { get; set; }
    /// <summary>True when <see cref="TenantId"/> is null (platform-level error).</summary>
    public bool IsPlatformError => TenantId is null;
}

/// <summary>Exception log list.</summary>
public class ExceptionLogList
{
    /// <summary>Gets or sets the items.</summary>
    public List<ExceptionLogViewModel> Items { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the unresolved.</summary>
    public int Unresolved { get; set; }
    /// <summary>Gets or sets the critical.</summary>
    public int Critical { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}

/// <summary>API health endpoint view model.</summary>
public class ApiHealthEndpointViewModel
{
    /// <summary>Gets or sets the endpoint.</summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>Gets or sets the request count.</summary>
    public int RequestCount { get; set; }
    /// <summary>Gets or sets the success pct.</summary>
    public decimal SuccessPct { get; set; }
    /// <summary>Gets or sets the failed pct.</summary>
    public decimal FailedPct { get; set; }
    /// <summary>Anything 1xx/3xx — treated as a holding state in the dashboard.</summary>
    public decimal PendingPct { get; set; }
    /// <summary>Gets or sets the others pct.</summary>
    public decimal OthersPct { get; set; }
    /// <summary>Gets or sets the last checked at.</summary>
    public DateTime? LastCheckedAt { get; set; }
    /// <summary>Gets or sets the last is healthy.</summary>
    public bool? LastIsHealthy { get; set; }
}

/// <summary>API health stats view model.</summary>
public class ApiHealthStatsViewModel
{
    /// <summary>Gets or sets the total API.</summary>
    public int TotalApi { get; set; }
    /// <summary>Gets or sets the total request count.</summary>
    public long TotalRequestCount { get; set; }
    /// <summary>Gets or sets the success pct.</summary>
    public decimal SuccessPct { get; set; }
    /// <summary>Gets or sets the pending pct.</summary>
    public decimal PendingPct { get; set; }
    /// <summary>Gets or sets the failed pct.</summary>
    public decimal FailedPct { get; set; }
    /// <summary>Gets or sets the others pct.</summary>
    public decimal OthersPct { get; set; }
    /// <summary>Gets or sets the endpoints.</summary>
    public List<ApiHealthEndpointViewModel> Endpoints { get; set; } = new();
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}

/// <summary>Kiosk inventory snapshot view model.</summary>
public class KioskInventorySnapshotViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }
    /// <summary>Gets or sets the kiosk code.</summary>
    public string KioskCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the metal.</summary>
    public string Metal { get; set; } = string.Empty;
    /// <summary>Gets or sets the weight g.</summary>
    public decimal WeightG { get; set; }
    /// <summary>Gets or sets the carat.</summary>
    public decimal? Carat { get; set; }
    /// <summary>Gets or sets the captured at.</summary>
    public DateTime CapturedAt { get; set; }
}

/// <summary>Kiosk inventory snapshot list.</summary>
public class KioskInventorySnapshotList
{
    /// <summary>Gets or sets the items.</summary>
    public List<KioskInventorySnapshotViewModel> Items { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}

// ─── Home dashboard ─────────────────────────────────────────────────────
/// <summary>Dashboard view model.</summary>
public class DashboardViewModel
{
    /// <summary>Gets or sets the active kiosk count.</summary>
    public int ActiveKioskCount { get; set; }
    /// <summary>Gets or sets the total customers.</summary>
    public int TotalCustomers { get; set; }
    /// <summary>Gets or sets the todays transaction count.</summary>
    public int TodaysTransactionCount { get; set; }
    /// <summary>Gets or sets the todays gross sales.</summary>
    public decimal TodaysGrossSales { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = "INR";

    /// <summary>Gets or sets the open tickets.</summary>
    public int OpenTickets { get; set; }
    /// <summary>Gets or sets the unresolved exceptions.</summary>
    public int UnresolvedExceptions { get; set; }

    /// <summary>Gets or sets the recent transactions.</summary>
    public List<RecentTransactionRow> RecentTransactions { get; set; } = new();
    /// <summary>Gets or sets the active kiosks.</summary>
    public List<ActiveKioskRow> ActiveKiosks { get; set; } = new();
}

/// <summary>Recent transaction row.</summary>
public class RecentTransactionRow
{
    /// <summary>Gets or sets the transaction code.</summary>
    public string TransactionCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the kind.</summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the total amount.</summary>
    public decimal TotalAmount { get; set; }
    /// <summary>Gets or sets the net payout.</summary>
    public decimal NetPayout { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the occurred on.</summary>
    public DateTime OccurredOn { get; set; }
}

/// <summary>Active kiosk row.</summary>
public class ActiveKioskRow
{
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the friendly name.</summary>
    public string FriendlyName { get; set; } = string.Empty;
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is maintenance.</summary>
    public bool IsMaintenance { get; set; }
    /// <summary>Gets or sets the last ping.</summary>
    public DateTime? LastPing { get; set; }
}

// ────────────────────────────────────────────────────────────────────────
// Sales (PreciousMetal + PawnSales)
// ────────────────────────────────────────────────────────────────────────

/// <summary>Row in the Sales/PreciousMetal + Sales/PawnSales tables.</summary>
public class SalesClientRowViewModel
{
    /// <summary>Gets or sets the transaction id.</summary>
    public Guid TransactionId { get; set; }
    /// <summary>Gets or sets the transaction code.</summary>
    public string TransactionCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the customer id.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the customer code.</summary>
    public string CustomerCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the mobile masked.</summary>
    public string MobileMasked { get; set; } = "***";
    /// <summary>Gets or sets the kind.</summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the source.</summary>
    public string? Source { get; set; }
    /// <summary>Gets or sets the total amount.</summary>
    public decimal TotalAmount { get; set; }
    /// <summary>Gets or sets the net payout.</summary>
    public decimal NetPayout { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is junk.</summary>
    public bool IsJunk { get; set; }
    /// <summary>Gets or sets the occurred on.</summary>
    public DateTime OccurredOn { get; set; }
}

/// <summary>List-with-stats model returned by the Sales service.</summary>
public class SalesClientList : ClientVm
{
    /// <summary>Gets or sets the items.</summary>
    public List<SalesClientRowViewModel> Items { get; set; } = new();
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
    /// <summary>Gets or sets the kind.</summary>
    public string Kind { get; set; } = string.Empty;
}

// ────────────────────────────────────────────────────────────────────────
// Reports (9 reports)
// ────────────────────────────────────────────────────────────────────────

/// <summary>Report filter view model.</summary>
public class ReportFilterViewModel
{
    /// <summary>Gets or sets the from.</summary>
    public DateTime? From { get; set; }
    /// <summary>Gets or sets the to.</summary>
    public DateTime? To { get; set; }
}

/// <summary>Daily sales report view model.</summary>
public class DailySalesReportViewModel : ReportFilterViewModel
{
    /// <summary>Gets or sets the rows.</summary>
    public List<DailySalesReportRow> Rows { get; set; } = new();
    /// <summary>Gets or sets the total payout.</summary>
    public decimal TotalPayout { get; set; }
    /// <summary>Gets or sets the total weight g.</summary>
    public decimal TotalWeightG { get; set; }
    /// <summary>Gets or sets the transaction count.</summary>
    public int TransactionCount { get; set; }
    /// <summary>Gets or sets the customer count.</summary>
    public int CustomerCount { get; set; }
}

/// <summary>Daily sales report row.</summary>
public class DailySalesReportRow
{
    /// <summary>Gets or sets the sale date.</summary>
    public DateTime SaleDate { get; set; }
    /// <summary>Gets or sets the kind.</summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Gets or sets the transaction count.</summary>
    public int TransactionCount { get; set; }
    /// <summary>Gets or sets the customer count.</summary>
    public int CustomerCount { get; set; }
    /// <summary>Gets or sets the total weight g.</summary>
    public decimal TotalWeightG { get; set; }
    /// <summary>Gets or sets the total payout.</summary>
    public decimal TotalPayout { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
}

/// <summary>Holding sales report view model.</summary>
public class HoldingSalesReportViewModel : ReportFilterViewModel
{
    /// <summary>Gets or sets the rows.</summary>
    public List<HoldingSalesReportRow> Rows { get; set; } = new();
    /// <summary>Gets or sets the total holding weight g.</summary>
    public decimal TotalHoldingWeightG { get; set; }
    /// <summary>Gets or sets the total sold weight g.</summary>
    public decimal TotalSoldWeightG { get; set; }
    /// <summary>Gets or sets the total sold payout.</summary>
    public decimal TotalSoldPayout { get; set; }
}

/// <summary>Holding sales report row.</summary>
public class HoldingSalesReportRow
{
    /// <summary>Gets or sets the as of date.</summary>
    public DateTime AsOfDate { get; set; }
    /// <summary>Gets or sets the metal.</summary>
    public string Metal { get; set; } = string.Empty;
    /// <summary>Gets or sets the holding weight g.</summary>
    public decimal HoldingWeightG { get; set; }
    /// <summary>Gets or sets the sold weight g.</summary>
    public decimal SoldWeightG { get; set; }
    /// <summary>Gets or sets the sold payout.</summary>
    public decimal SoldPayout { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
}

/// <summary>Carat weight report view model.</summary>
public class CaratWeightReportViewModel : ReportFilterViewModel
{
    /// <summary>Gets or sets the rows.</summary>
    public List<CaratWeightReportRow> Rows { get; set; } = new();
    /// <summary>Gets or sets the total weight g.</summary>
    public decimal TotalWeightG { get; set; }
    /// <summary>Gets or sets the total items.</summary>
    public int TotalItems { get; set; }
}

/// <summary>Carat weight report row.</summary>
public class CaratWeightReportRow
{
    /// <summary>Gets or sets the billed karat.</summary>
    public decimal BilledKarat { get; set; }
    /// <summary>Gets or sets the metal.</summary>
    public string Metal { get; set; } = string.Empty;
    /// <summary>Gets or sets the total weight g.</summary>
    public decimal TotalWeightG { get; set; }
    /// <summary>Gets or sets the item count.</summary>
    public int ItemCount { get; set; }
}

/// <summary>Total expense report view model.</summary>
public class TotalExpenseReportViewModel : ReportFilterViewModel
{
    /// <summary>Gets or sets the rows.</summary>
    public List<TotalExpenseReportRow> Rows { get; set; } = new();
    /// <summary>Gets or sets the total amount.</summary>
    public decimal TotalAmount { get; set; }
    /// <summary>Gets or sets a value indicating whether expense table missing.</summary>
    public bool ExpenseTableMissing { get; set; }
}

/// <summary>Total expense report row.</summary>
public class TotalExpenseReportRow
{
    /// <summary>Gets or sets the expense date.</summary>
    public DateTime ExpenseDate { get; set; }
    /// <summary>Gets or sets the amount.</summary>
    public decimal Amount { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
}

/// <summary>Worth report view model.</summary>
public class WorthReportViewModel
{
    /// <summary>Gets or sets the as of.</summary>
    public DateTime AsOf { get; set; }
    /// <summary>Gets or sets the rows.</summary>
    public List<WorthReportRow> Rows { get; set; } = new();
    /// <summary>Gets or sets the total estimated worth.</summary>
    public decimal TotalEstimatedWorth { get; set; }
}

/// <summary>Worth report row.</summary>
public class WorthReportRow
{
    /// <summary>Gets or sets the metal.</summary>
    public string Metal { get; set; } = string.Empty;
    /// <summary>Gets or sets the avg carat.</summary>
    public decimal? AvgCarat { get; set; }
    /// <summary>Gets or sets the total weight g.</summary>
    public decimal TotalWeightG { get; set; }
    /// <summary>Gets or sets the latest rate per gram.</summary>
    public decimal LatestRatePerGram { get; set; }
    /// <summary>Gets or sets the estimated worth.</summary>
    public decimal EstimatedWorth { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
}

/// <summary>Sales payout report view model.</summary>
public class SalesPayoutReportViewModel : ReportFilterViewModel
{
    /// <summary>Gets or sets the rows.</summary>
    public List<SalesPayoutReportRow> Rows { get; set; } = new();
    /// <summary>Gets or sets the total net payout.</summary>
    public decimal TotalNetPayout { get; set; }
    /// <summary>Gets or sets the total fees.</summary>
    public decimal TotalFees { get; set; }
}

/// <summary>Sales payout report row.</summary>
public class SalesPayoutReportRow
{
    /// <summary>Gets or sets the occurred on.</summary>
    public DateTime OccurredOn { get; set; }
    /// <summary>Gets or sets the transaction code.</summary>
    public string TransactionCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the amount.</summary>
    public decimal Amount { get; set; }
    /// <summary>Gets or sets the fees.</summary>
    public decimal Fees { get; set; }
    /// <summary>Gets or sets the net payout.</summary>
    public decimal NetPayout { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the transaction status.</summary>
    public string TransactionStatus { get; set; } = string.Empty;
    /// <summary>Gets or sets the payout status.</summary>
    public string? PayoutStatus { get; set; }
    /// <summary>Gets or sets the payout sent at.</summary>
    public DateTime? PayoutSentAt { get; set; }
    /// <summary>Gets or sets the payout settled at.</summary>
    public DateTime? PayoutSettledAt { get; set; }
}

/// <summary>Expected profit report view model.</summary>
public class ExpectedProfitReportViewModel : ReportFilterViewModel
{
    /// <summary>Gets or sets the rows.</summary>
    public List<ExpectedProfitReportRow> Rows { get; set; } = new();
    /// <summary>Gets or sets the total gross sales.</summary>
    public decimal TotalGrossSales { get; set; }
    /// <summary>Gets or sets the total cost of acquisition.</summary>
    public decimal TotalCostOfAcquisition { get; set; }
    /// <summary>Gets or sets the total expected profit.</summary>
    public decimal TotalExpectedProfit { get; set; }
}

/// <summary>Expected profit report row.</summary>
public class ExpectedProfitReportRow
{
    /// <summary>Gets or sets the profit date.</summary>
    public DateTime ProfitDate { get; set; }
    /// <summary>Gets or sets the gross sales.</summary>
    public decimal GrossSales { get; set; }
    /// <summary>Gets or sets the cost of acquisition.</summary>
    public decimal CostOfAcquisition { get; set; }
    /// <summary>Gets or sets the expected profit.</summary>
    public decimal ExpectedProfit { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
}

/// <summary>Profit after expense report view model.</summary>
public class ProfitAfterExpenseReportViewModel : ReportFilterViewModel
{
    /// <summary>Gets or sets the rows.</summary>
    public List<ProfitAfterExpenseReportRow> Rows { get; set; } = new();
    /// <summary>Gets or sets the total gross sales.</summary>
    public decimal TotalGrossSales { get; set; }
    /// <summary>Gets or sets the total cost of acquisition.</summary>
    public decimal TotalCostOfAcquisition { get; set; }
    /// <summary>Gets or sets the total expected profit.</summary>
    public decimal TotalExpectedProfit { get; set; }
    /// <summary>Gets or sets the total expenses.</summary>
    public decimal TotalExpenses { get; set; }
    /// <summary>Gets or sets the total net profit.</summary>
    public decimal TotalNetProfit { get; set; }
}

/// <summary>Profit after expense report row.</summary>
public class ProfitAfterExpenseReportRow
{
    /// <summary>Gets or sets the profit date.</summary>
    public DateTime ProfitDate { get; set; }
    /// <summary>Gets or sets the gross sales.</summary>
    public decimal GrossSales { get; set; }
    /// <summary>Gets or sets the cost of acquisition.</summary>
    public decimal CostOfAcquisition { get; set; }
    /// <summary>Gets or sets the expected profit.</summary>
    public decimal ExpectedProfit { get; set; }
    /// <summary>Gets or sets the expenses.</summary>
    public decimal Expenses { get; set; }
    /// <summary>Gets or sets the net profit.</summary>
    public decimal NetProfit { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
}

/// <summary>Offer report view model.</summary>
public class OfferReportViewModel : ReportFilterViewModel
{
    /// <summary>Gets or sets the rows.</summary>
    public List<OfferReportItem> Rows { get; set; } = new();
    /// <summary>Gets or sets the active count.</summary>
    public int ActiveCount { get; set; }
    /// <summary>Gets or sets the inactive count.</summary>
    public int InactiveCount { get; set; }
}

/// <summary>Offer report item.</summary>
public class OfferReportItem
{
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the discount pct.</summary>
    public decimal? DiscountPct { get; set; }
    /// <summary>Gets or sets the validity start.</summary>
    public DateTime ValidityStart { get; set; }
    /// <summary>Gets or sets the validity end.</summary>
    public DateTime ValidityEnd { get; set; }
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; }
}

// ─── Operations module ─────────────────────────────────────────────────

/// <summary>Deployment ticket view model.</summary>
public class DeploymentTicketViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid? KioskId { get; set; }
    /// <summary>Gets or sets the kiosk code.</summary>
    public string? KioskCode { get; set; }
    /// <summary>Gets or sets the store id.</summary>
    public Guid? StoreId { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "open";
    /// <summary>Gets or sets the priority.</summary>
    public string Priority { get; set; } = "normal";
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the assigned technician id.</summary>
    public Guid? AssignedTechnicianId { get; set; }
    /// <summary>Gets or sets the technician name.</summary>
    public string? TechnicianName { get; set; }
    /// <summary>Gets or sets the opened at.</summary>
    public DateTime OpenedAt { get; set; }
    /// <summary>Gets or sets the closed at.</summary>
    public DateTime? ClosedAt { get; set; }
}

/// <summary>Deployment ticket list.</summary>
public class DeploymentTicketList
{
    /// <summary>Gets or sets the items.</summary>
    public List<DeploymentTicketViewModel> Items { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the open.</summary>
    public int Open { get; set; }
    /// <summary>Gets or sets the in progress.</summary>
    public int InProgress { get; set; }
    /// <summary>Gets or sets the closed.</summary>
    public int Closed { get; set; }
    /// <summary>Gets or sets the resolved.</summary>
    public int Resolved { get; set; }
    /// <summary>Gets or sets the reopened.</summary>
    public int Reopened { get; set; }
    /// <summary>Gets or sets the others.</summary>
    public int Others { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}

/// <summary>Maintenance ticket view model.</summary>
public class MaintenanceTicketViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }
    /// <summary>Gets or sets the kiosk code.</summary>
    public string? KioskCode { get; set; }
    /// <summary>Gets or sets the device id.</summary>
    public Guid? DeviceId { get; set; }
    /// <summary>Gets or sets the ticket type.</summary>
    public string TicketType { get; set; } = "preventive";
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the priority.</summary>
    public string Priority { get; set; } = "normal";
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "open";
    /// <summary>Gets or sets the technician id.</summary>
    public Guid? TechnicianId { get; set; }
    /// <summary>Gets or sets the technician name.</summary>
    public string? TechnicianName { get; set; }
    /// <summary>Gets or sets the scheduled at.</summary>
    public DateTime? ScheduledAt { get; set; }
    /// <summary>Gets or sets the completed at.</summary>
    public DateTime? CompletedAt { get; set; }
    /// <summary>Gets or sets the remarks.</summary>
    public string? Remarks { get; set; }
}

/// <summary>Maintenance ticket list.</summary>
public class MaintenanceTicketList
{
    /// <summary>Gets or sets the items.</summary>
    public List<MaintenanceTicketViewModel> Items { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the open.</summary>
    public int Open { get; set; }
    /// <summary>Gets or sets the in progress.</summary>
    public int InProgress { get; set; }
    /// <summary>Gets or sets the closed.</summary>
    public int Closed { get; set; }
    /// <summary>Gets or sets the resolved.</summary>
    public int Resolved { get; set; }
    /// <summary>Gets or sets the reopened.</summary>
    public int Reopened { get; set; }
    /// <summary>Gets or sets the others.</summary>
    public int Others { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}

/// <summary>Technician view model.</summary>
public class TechnicianViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the cluster id.</summary>
    public Guid ClusterId { get; set; }
    /// <summary>Gets or sets the cluster code.</summary>
    public string? ClusterCode { get; set; }
    /// <summary>Gets or sets the cluster name.</summary>
    public string? ClusterName { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "active";
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Gets or sets the created on.</summary>
    public DateTime CreatedOn { get; set; }
}

/// <summary>Technician cluster view model.</summary>
public class TechnicianClusterViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>Technician list.</summary>
public class TechnicianList
{
    /// <summary>Gets or sets the items.</summary>
    public List<TechnicianViewModel> Items { get; set; } = new();
    /// <summary>Gets or sets the clusters.</summary>
    public List<TechnicianClusterViewModel> Clusters { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the active.</summary>
    public int Active { get; set; }
    /// <summary>Gets or sets the in active.</summary>
    public int InActive { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}

/// <summary>Collection run view model.</summary>
public class CollectionRunViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the run date.</summary>
    public DateTime RunDate { get; set; }
    /// <summary>Gets or sets the lead technician id.</summary>
    public Guid? LeadTechnicianId { get; set; }
    /// <summary>Gets or sets the lead technician name.</summary>
    public string? LeadTechnicianName { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "planned";
    /// <summary>Gets or sets the total amount.</summary>
    public decimal TotalAmount { get; set; }
    /// <summary>Gets or sets the total items.</summary>
    public int TotalItems { get; set; }
    /// <summary>Gets or sets the started at.</summary>
    public DateTime? StartedAt { get; set; }
    /// <summary>Gets or sets the completed at.</summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>Collection ticket view model.</summary>
public class CollectionTicketViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the collection run id.</summary>
    public Guid CollectionRunId { get; set; }
    /// <summary>Gets or sets the run code.</summary>
    public string? RunCode { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }
    /// <summary>Gets or sets the kiosk code.</summary>
    public string? KioskCode { get; set; }
    /// <summary>Gets or sets the amount collected.</summary>
    public decimal AmountCollected { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the items collected.</summary>
    public int ItemsCollected { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "pending";
    /// <summary>Gets or sets the collected at.</summary>
    public DateTime? CollectedAt { get; set; }
    /// <summary>Gets or sets the signed off by user id.</summary>
    public Guid? SignedOffByUserId { get; set; }
}

/// <summary>Collection ticket list.</summary>
public class CollectionTicketList
{
    /// <summary>Gets or sets the runs.</summary>
    public List<CollectionRunViewModel> Runs { get; set; } = new();
    /// <summary>Gets or sets the tickets.</summary>
    public List<CollectionTicketViewModel> Tickets { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}

/// <summary>Kiosk location view model.</summary>
public class KioskLocationViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the store id.</summary>
    public Guid? StoreId { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;
    /// <summary>Gets or sets the address.</summary>
    public string? Address { get; set; }
    /// <summary>Gets or sets the latitude.</summary>
    public decimal? Latitude { get; set; }
    /// <summary>Gets or sets the longitude.</summary>
    public decimal? Longitude { get; set; }
    /// <summary>Gets or sets the region.</summary>
    public string? Region { get; set; }
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>Kiosk location list.</summary>
public class KioskLocationList
{
    /// <summary>Gets or sets the items.</summary>
    public List<KioskLocationViewModel> Items { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the active.</summary>
    public int Active { get; set; }
    /// <summary>Gets or sets the in active.</summary>
    public int InActive { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
}

/// <summary>Kiosk security token view model.</summary>
public class KioskSecurityTokenViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }
    /// <summary>Gets or sets the kiosk code.</summary>
    public string? KioskCode { get; set; }
    /// <summary>Gets or sets the token hash preview.</summary>
    public string TokenHashPreview { get; set; } = string.Empty;   // first 8 hex chars; raw token never stored
    /// <summary>Gets or sets the issued at.</summary>
    public DateTime IssuedAt { get; set; }
    /// <summary>Gets or sets the expires at.</summary>
    public DateTime ExpiresAt { get; set; }
    /// <summary>Gets or sets the revoked at.</summary>
    public DateTime? RevokedAt { get; set; }
    /// <summary>Gets or sets the last used at.</summary>
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>Kiosk security token list.</summary>
public class KioskSecurityTokenList
{
    /// <summary>Gets or sets the items.</summary>
    public List<KioskSecurityTokenViewModel> Items { get; set; } = new();
    /// <summary>Gets or sets the total.</summary>
    public int Total { get; set; }
    /// <summary>Gets or sets the active.</summary>
    public int Active { get; set; }
    /// <summary>Gets or sets the revoked.</summary>
    public int Revoked { get; set; }
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }
    /// <summary>Gets or sets the page no.</summary>
    public int PageNo { get; set; }
    /// <summary>Gets or sets the page count.</summary>
    public int PageCount { get; set; }
    /// <summary>Gets or sets the total page.</summary>
    public int TotalPage { get; set; }
    /// <summary>Gets or sets the newly issued raw token.</summary>
    public string? NewlyIssuedRawToken { get; set; }                // surfaced ONCE after an Issue action
    /// <summary>Gets or sets the newly issued kiosk id.</summary>
    public Guid? NewlyIssuedKioskId { get; set; }
}

/// <summary>System health snapshot view model.</summary>
public class SystemHealthSnapshotViewModel
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the captured at.</summary>
    public DateTimeOffset CapturedAt { get; set; }
    /// <summary>Gets or sets the total cities.</summary>
    public int TotalCities { get; set; }
    /// <summary>Gets or sets the total kiosks.</summary>
    public int TotalKiosks { get; set; }
    /// <summary>Gets or sets the functional count.</summary>
    public int FunctionalCount { get; set; }
    /// <summary>Gets or sets the non functional count.</summary>
    public int NonFunctionalCount { get; set; }
    /// <summary>Gets or sets the avg uptime pct.</summary>
    public decimal AvgUptimePct { get; set; }
}

/// <summary>System health dashboard view model.</summary>
public class SystemHealthDashboardViewModel
{
    /// <summary>Gets or sets the latest.</summary>
    public SystemHealthSnapshotViewModel? Latest { get; set; }
    /// <summary>Gets or sets the trend24h.</summary>
    public List<SystemHealthSnapshotViewModel> Trend24h { get; set; } = new();
}
