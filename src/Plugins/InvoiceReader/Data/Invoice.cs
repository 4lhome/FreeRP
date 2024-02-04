using FreeRP.GrpcService.Core;
using FreeRP.GrpcService.Database;
using FreeRP.GrpcService.Content;

namespace InvoiceReader.Data
{
    public class Invoice
    {
        public string InvoiceId { get; set; } = Guid.NewGuid().ToString();
        public string InvoiceNumber { get; set; } = string.Empty;
        public FrpUtcDateTime? InvoiceDate { get; set; }
        public FrpUtcDateTime? InvoiceDueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public FrpFile? File { get; set; }
        public string FileText { get; set; } = string.Empty;
        public List<InvoicePosition> Positions { get; set; } = [];
    }

    public class InvoicePosition
    {
        public decimal Amount { get; set; }
        public int AccountNumber { get; set; }
        public int OffsettingAccountNumber { get; set; }
        public int CostCategoryId { get; set; }
        public int CostCategoryId2 { get; set; }
        public FrpUtcDateTime? Date { get; set; }
        public string BookingText { get; set; } = string.Empty;
        public double Tax { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
    }

    public class InvoiceImporter
    {
        public string FileName { get; set; } = string.Empty;
        public string FileAsBase64 { get; set; } = string.Empty;
        public Dictionary<int, FreeRP.Net.Client.Data.PdfPage> Images { get; set; } = [];
    }
}
