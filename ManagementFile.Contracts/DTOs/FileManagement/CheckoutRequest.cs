using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.FileManagement
{
    /// <summary>
    /// Checkout request DTO
    /// </summary>
    public class CheckoutRequest
    {
        public int ExpectedCheckinHours { get; set; } = 24;
    }
}
