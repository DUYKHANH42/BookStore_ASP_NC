using BookStore.Application.DTO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class InvoiceService
    {
        public byte[] GenerateInvoicePdf(OrderFullDetailDTO order, string logoPath)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    // Header
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("LUMEN BOOKSTORE").FontSize(24).SemiBold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Hệ thống quản trị sách hàng đầu").FontSize(10).FontColor(Colors.Grey.Medium);
                            col.Item().Text("Hotline: 1900 1234 | Email: support@lumen.vn").FontSize(9);
                        });

                        if (System.IO.File.Exists(logoPath))
                        {
                            row.ConstantItem(100).Image(logoPath);
                        }
                    });

                    // Content
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().PaddingBottom(20).AlignCenter().Text("HÓA ĐƠN BÁN HÀNG").FontSize(20).ExtraBold().LetterSpacing(0.1f);
                        
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("KHÁCH HÀNG").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                                c.Item().Text(order.ShippingName).SemiBold();
                                c.Item().Text($"SĐT: {order.ShippingPhone}");
                                c.Item().Text($"ĐC: {order.ShippingAddress}");
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("MÃ ĐƠN HÀNG").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                                c.Item().Text($"#{order.OrderNumber}").SemiBold();
                                c.Item().PaddingTop(5).Text("NGÀY ĐẶT").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                                c.Item().Text(order.CreatedAt.ToString("dd/MM/yyyy HH:mm"));
                            });
                        });

                        col.Item().PaddingVertical(20).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Sản phẩm");
                                header.Cell().Element(CellStyle).AlignCenter().Text("Số lượng");
                                header.Cell().Element(CellStyle).AlignRight().Text("Đơn giá");
                                header.Cell().Element(CellStyle).AlignRight().Text("Thành tiền");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var item in order.Items)
                            {
                                table.Cell().Element(CellStyle).Text(item.ProductName);
                                table.Cell().Element(CellStyle).AlignCenter().Text(item.Quantity.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text(item.Price.ToString("N0"));
                                table.Cell().Element(CellStyle).AlignRight().Text(item.SubTotal.ToString("N0"));

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                }
                            }
                        });

                        col.Item().AlignRight().Text(x =>
                        {
                            x.Span("TỔNG CỘNG THANH TOÁN: ").FontSize(12).SemiBold();
                            x.Span($"{order.TotalPrice:N0} VNĐ").FontSize(14).ExtraBold().FontColor(Colors.Blue.Medium);
                        });

                        // Signatures
                        col.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("Khách hàng ký tên").SemiBold();
                                c.Item().Text("(Ký và ghi rõ họ tên)").FontSize(8).FontColor(Colors.Grey.Medium);
                            });

                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("Đại diện cửa hàng").SemiBold();
                                c.Item().PaddingTop(10).Border(2).BorderColor(Colors.Red.Medium).Padding(5).Column(cc =>
                                {
                                    cc.Item().AlignCenter().Text("ĐÃ XÁC THỰC").FontSize(8).Bold().FontColor(Colors.Red.Medium);
                                    cc.Item().AlignCenter().Text("LUMEN BOOKSTORE").FontSize(7).Bold().FontColor(Colors.Red.Medium);
                                    cc.Item().AlignCenter().Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(7).FontColor(Colors.Red.Medium);
                                });
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Cảm ơn quý khách đã mua sắm tại ").FontSize(9);
                        x.Span("Lumen Bookstore").FontSize(9).SemiBold();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateInventoryReceiptPdf(InventoryReceiptDTO receipt, string logoPath)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    // Header
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("LUMEN BOOKSTORE").FontSize(24).SemiBold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Hệ thống quản trị kho chuyên nghiệp").FontSize(10).FontColor(Colors.Grey.Medium);
                        });

                        if (System.IO.File.Exists(logoPath))
                        {
                            row.ConstantItem(100).Image(logoPath);
                        }
                    });

                    // Content
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().PaddingBottom(20).AlignCenter().Text("PHIẾU NHẬP KHO").FontSize(20).ExtraBold().LetterSpacing(0.1f);
                        
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("THÔNG TIN NHÀ CUNG CẤP").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                                c.Item().Text(receipt.SupplierName).SemiBold();
                                c.Item().Text($"Ngày nhập: {receipt.ReceivedDate:dd/MM/yyyy HH:mm}");
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("MÃ PHIẾU").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                                c.Item().Text($"#PNK-{receipt.Id}").SemiBold();
                                c.Item().PaddingTop(5).Text("NGƯỜI THỰC HIỆN").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                                c.Item().Text(receipt.EmployeeName);
                            });
                        });

                        col.Item().PaddingVertical(20).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Sản phẩm / SKU");
                                header.Cell().Element(CellStyle).AlignCenter().Text("Số lượng");
                                header.Cell().Element(CellStyle).AlignRight().Text("Giá nhập");
                                header.Cell().Element(CellStyle).AlignRight().Text("Thành tiền");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var item in receipt.Details)
                            {
                                table.Cell().Element(CellStyle).Column(c => {
                                    c.Item().Text(item.ProductName);
                                    c.Item().Text(item.SKU).FontSize(8).FontColor(Colors.Grey.Medium);
                                });
                                table.Cell().Element(CellStyle).AlignCenter().Text(item.Quantity.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text(item.ImportPrice.ToString("N0"));
                                table.Cell().Element(CellStyle).AlignRight().Text(item.SubTotal.ToString("N0"));

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                }
                            }
                        });

                        col.Item().AlignRight().Text(x =>
                        {
                            x.Span("TỔNG GIÁ TRỊ NHẬP: ").FontSize(12).SemiBold();
                            x.Span($"{receipt.TotalAmount:N0} VNĐ").FontSize(14).ExtraBold().FontColor(Colors.Blue.Medium);
                        });

                        if (!string.IsNullOrEmpty(receipt.Notes))
                        {
                            col.Item().PaddingTop(10).Text(x =>
                            {
                                x.Span("Ghi chú: ").SemiBold();
                                x.Span(receipt.Notes).Italic();
                            });
                        }

                        // Signatures
                        col.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("Đại diện nhà cung cấp").SemiBold();
                                c.Item().Text("(Ký và ghi rõ họ tên)").FontSize(8).FontColor(Colors.Grey.Medium);
                            });

                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("Thủ kho xác nhận").SemiBold();
                                c.Item().PaddingTop(10).Border(2).BorderColor(Colors.Green.Medium).Padding(5).Column(cc =>
                                {
                                    cc.Item().AlignCenter().Text("ĐÃ NHẬP KHO").FontSize(8).Bold().FontColor(Colors.Green.Medium);
                                    cc.Item().AlignCenter().Text("LUMEN BOOKSTORE").FontSize(7).Bold().FontColor(Colors.Green.Medium);
                                    cc.Item().AlignCenter().Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(7).FontColor(Colors.Green.Medium);
                                });
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Phiếu nhập kho - Lưu hành nội bộ - Lumen Bookstore").FontSize(9);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
