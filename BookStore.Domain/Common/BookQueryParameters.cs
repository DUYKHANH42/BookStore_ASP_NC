using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Common
{
    public class BookQueryParameters : BaseQueryParameters
    {
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        // Sau này muốn thêm lọc theo Author, Year... thì viết vào đây
    }
}
