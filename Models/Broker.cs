using System.ComponentModel.DataAnnotations;
namespace ShopBroker.Models;

public class InsuranceCompany
{
    public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
}

public class InsuranceClient
{
    public int Id { get; set; }
    [Required] public string FullName { get; set; } = "";
    [Required] public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public List<Policy> Policies { get; set; } = new();
}

public class Policy
{
    public int Id { get; set; }
    [Required] public string PolicyNumber { get; set; } = "";
    public int InsuranceClientId { get; set; }
    public InsuranceClient? Client { get; set; }
    public int InsuranceCompanyId { get; set; }
    public InsuranceCompany? Company { get; set; }
    public string PolicyType { get; set; } = "Life"; // Life, Health, Motor, Property
    public decimal SumAssured { get; set; }
    public decimal PremiumAmount { get; set; }
    public string PremiumFrequency { get; set; } = "Yearly";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime NextPremiumDue { get; set; }
    public decimal CommissionPercent { get; set; }
    public string Status { get; set; } = "Active"; // Active, Lapsed, Expired, Claimed
    public List<PremiumPayment> Payments { get; set; } = new();
}

public class PremiumPayment
{
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public Policy? Policy { get; set; }
    public DateTime PaidOn { get; set; }
    public decimal Amount { get; set; }
    public decimal CommissionEarned { get; set; }
}
