using System.Collections.Generic;

namespace StackFlow.ViewModels
{
    /// <summary>
    /// ViewModel to hold data for both Active Users and Pending Verification Users tables
    /// on the User Reports page, including their respective pagination details.
    /// </summary>
    public class UserReportsCombinedViewModel
    {
        public List<UserReportViewModel> ActiveUsers { get; set; } = new List<UserReportViewModel>();
        public List<UserReportViewModel> PendingUsers { get; set; } = new List<UserReportViewModel>();

        // Pagination properties for Active Users table
        public int ActiveUsersCurrentPage { get; set; } = 1;
        public int ActiveUsersPageSize { get; set; } = 10;
        public int ActiveUsersTotalPages { get; set; }
        public string ActiveUsersSearchQuery { get; set; }

        // Pagination properties for Pending Users table
        public int PendingUsersCurrentPage { get; set; } = 1;
        public int PendingUsersPageSize { get; set; } = 10;
        public int PendingUsersTotalPages { get; set; }
    }
}
