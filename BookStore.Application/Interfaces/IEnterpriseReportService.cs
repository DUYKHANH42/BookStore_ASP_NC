using BookStore.Application.DTO;
using System.Threading.Tasks;

namespace BookStore.Application.Interfaces
{
    public interface IEnterpriseReportService
    {
        Task<EnterpriseReportDTO> GetReportAsync(ReportFilterDTO filter);
    }
}
