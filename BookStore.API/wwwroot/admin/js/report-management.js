let reportRevenueChart;
let reportStatusChart;
let reportCustomerChart;
let currentReport = window.enterpriseReportSeed;
const reportChartColors = {
    blue: '#2563eb',
    blueSoft: 'rgba(37, 99, 235, .1)',
    emerald: '#10b981',
    emeraldSoft: 'rgba(16, 185, 129, .12)',
    rose: '#e11d48',
    amber: '#f59e0b',
    slate: '#0f172a',
    slateMuted: '#94a3b8',
    grid: 'rgba(148, 163, 184, .18)'
};

$(function () {
    renderEnterpriseReport(currentReport);

    $('#btnApplyReportFilter').on('click', function () {
        loadEnterpriseReport();
    });

    $('#btnExportExcel').on('click', function () {
        window.location.href = '/Admin/Report/ExportExcel?' + buildReportQueryString();
    });

    $('#btnExportPdf').on('click', function () {
        window.location.href = '/Admin/Report/ExportPdf?' + buildReportQueryString();
    });
});

function collectReportFilter() {
    return {
        FromDate: $('#reportFromDate').val(),
        ToDate: $('#reportToDate').val(),
        Period: $('#reportPeriod').val(),
        CategoryId: $('#reportCategoryId').val(),
        ProductId: $('#reportProductId').val(),
        Status: $('#reportStatus').val()
    };
}

function buildReportQueryString() {
    const params = new URLSearchParams();
    const filter = collectReportFilter();
    Object.keys(filter).forEach(key => {
        if (filter[key]) {
            params.append(key, filter[key]);
        }
    });
    return params.toString();
}

function loadEnterpriseReport() {
    const $btn = $('#btnApplyReportFilter');
    const originalHtml = $btn.html();
    $btn.prop('disabled', true).html('<span class="material-symbols-outlined">hourglass_top</span> Đang tải');

    $.get('/Admin/Report/Data?' + buildReportQueryString(), function (report) {
        currentReport = report;
        renderEnterpriseReport(report);
    }).fail(function () {
        Swal.fire('Không thể tải báo cáo', 'Vui lòng kiểm tra bộ lọc và thử lại.', 'error');
    }).always(function () {
        $btn.prop('disabled', false).html(originalHtml);
    });
}

function renderEnterpriseReport(report) {
    if (!report) return;

    renderKpis(report.kpis);
    renderCharts(report);
    renderProductRows(report.productRows || []);
    renderCustomerRows(report.customerRows || []);
    renderFlashSaleRows(report.flashSaleRows || []);
    renderGeoRows(report.customerGeography || []);
}

function renderKpis(kpis) {
    $('#kpiNetRevenue').text(formatMoney(kpis.netRevenue));
    $('#kpiGrowth').text(`So với kỳ trước: ${formatPercent(kpis.revenueGrowthRate)}`);
    $('#kpiTotalOrders').text(formatNumber(kpis.totalOrders));
    $('#kpiAov').text(`AOV: ${formatMoney(kpis.averageOrderValue)}`);
    $('#kpiCancelRate').text(formatPercent(kpis.cancellationRate));
    $('#kpiCancelledOrders').text(`${formatNumber(kpis.cancelledOrders)} đơn đã hủy`);
    $('#kpiNewCustomers').text(formatNumber(kpis.newCustomers));
    $('#kpiReturningCustomers').text(`${formatNumber(kpis.returningCustomers)} khách quay lại`);
    $('#kpiUnitsSold').text(formatNumber(kpis.totalUnitsSold));
}

function renderCharts(report) {
    const revenueLabels = (report.revenueTrends || []).map(x => x.label);
    const revenueData = (report.revenueTrends || []).map(x => x.revenue);
    const orderData = (report.revenueTrends || []).map(x => x.orderCount);
    const statusLabels = (report.orderStatuses || []).map(x => x.status);
    const statusData = (report.orderStatuses || []).map(x => x.count);
    const customerLabels = (report.newCustomerTrends || []).map(x => x.label);
    const customerData = (report.newCustomerTrends || []).map(x => x.count);

    if (reportRevenueChart) reportRevenueChart.destroy();
    if (reportStatusChart) reportStatusChart.destroy();
    if (reportCustomerChart) reportCustomerChart.destroy();

    reportRevenueChart = new Chart($('#reportRevenueChart')[0].getContext('2d'), {
        type: 'bar',
        data: {
            labels: revenueLabels,
            datasets: [
                {
                    label: 'Doanh thu',
                    data: revenueData,
                    backgroundColor: reportChartColors.blue,
                    borderRadius: 14,
                    borderSkipped: false,
                    yAxisID: 'y'
                },
                {
                    label: 'Số đơn',
                    data: orderData,
                    type: 'line',
                    borderColor: reportChartColors.slate,
                    backgroundColor: 'transparent',
                    borderWidth: 3,
                    tension: .35,
                    yAxisID: 'y1'
                }
            ]
        },
        options: getRevenueChartOptions()
    });

    reportStatusChart = new Chart($('#reportStatusChart')[0].getContext('2d'), {
        type: 'doughnut',
        data: {
            labels: statusLabels,
            datasets: [{
                data: statusData,
                backgroundColor: [
                    reportChartColors.blue,
                    reportChartColors.emerald,
                    reportChartColors.amber,
                    reportChartColors.rose,
                    '#64748b'
                ],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '72%',
            plugins: { legend: { position: 'bottom', labels: { font: { weight: 'bold', size: 11 } } } }
        }
    });

    reportCustomerChart = new Chart($('#reportCustomerChart')[0].getContext('2d'), {
        type: 'line',
        data: {
            labels: customerLabels,
            datasets: [{
                label: 'Khách hàng mới',
                data: customerData,
                borderColor: reportChartColors.emerald,
                backgroundColor: reportChartColors.emeraldSoft,
                borderWidth: 3,
                fill: true,
                tension: .35
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: {
                x: { grid: { display: false }, ticks: { color: reportChartColors.slateMuted, font: { weight: 'bold' } } },
                y: { beginAtZero: true, grid: { color: reportChartColors.grid }, ticks: { precision: 0, color: reportChartColors.slateMuted } }
            }
        }
    });
}

function getRevenueChartOptions() {
    return {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        plugins: {
            legend: { position: 'top', align: 'end', labels: { font: { weight: 'bold', size: 11 } } },
            tooltip: {
                callbacks: {
                    label: function (context) {
                        return context.dataset.yAxisID === 'y'
                            ? `Doanh thu: ${formatMoney(context.raw)}`
                            : `Số đơn: ${formatNumber(context.raw)}`;
                    }
                }
            }
        },
        scales: {
            x: { grid: { display: false }, ticks: { color: reportChartColors.slateMuted, font: { weight: 'bold' } } },
            y: {
                beginAtZero: true,
                grid: { color: reportChartColors.grid },
                ticks: { color: reportChartColors.slateMuted, callback: value => compactMoney(value) }
            },
            y1: {
                beginAtZero: true,
                position: 'right',
                grid: { drawOnChartArea: false },
                ticks: { precision: 0, color: reportChartColors.slateMuted }
            }
        }
    };
}

function renderProductRows(rows) {
    const $body = $('#productReportRows');
    $body.empty();

    if (!rows.length) {
        $body.append(emptyRow(6));
        return;
    }

    rows.slice(0, 12).forEach(item => {
        $body.append(`
            <tr>
                <td>${escapeHtml(item.productName)}</td>
                <td>${escapeHtml(item.categoryName)}</td>
                <td>${formatNumber(item.unitsSold)}</td>
                <td>${formatMoney(item.revenue)}</td>
                <td>${formatNumber(item.stock)}</td>
                <td><span class="report-pill">${escapeHtml(item.velocityLabel)} · ${formatPercent(item.sellThroughRate)}</span></td>
            </tr>
        `);
    });
}

function renderCustomerRows(rows) {
    const $body = $('#customerReportRows');
    $body.empty();

    if (!rows.length) {
        $body.append(emptyRow(5));
        return;
    }

    rows.slice(0, 12).forEach(item => {
        $body.append(`
            <tr>
                <td>${escapeHtml(item.customerName)}<div class="report-muted-line">${escapeHtml(item.email || '')}</div></td>
                <td>${formatNumber(item.orderCount)}</td>
                <td>${formatMoney(item.totalSpent)}</td>
                <td><span class="report-pill">${escapeHtml(item.segment)}</span></td>
                <td>${escapeHtml(item.location)}</td>
            </tr>
        `);
    });
}

function renderFlashSaleRows(rows) {
    const $body = $('#flashSaleReportRows');
    $body.empty();

    if (!rows.length) {
        $body.append(emptyRow(6));
        return;
    }

    rows.slice(0, 12).forEach(item => {
        $body.append(`
            <tr>
                <td>${escapeHtml(item.campaignName)}</td>
                <td>${formatNumber(item.soldCount)}</td>
                <td>${formatNumber(item.saleStock)}</td>
                <td>${formatPercent(item.soldStockRatio)}</td>
                <td>${formatMoney(item.revenue)}</td>
                <td><span class="report-pill">${escapeHtml(item.performanceLabel)}</span></td>
            </tr>
        `);
    });
}

function renderGeoRows(rows) {
    const $list = $('#geoReportRows');
    $list.empty();

    if (!rows.length) {
        $list.append('<div class="geo-item"><strong>Chưa có dữ liệu</strong><span>0</span></div>');
        return;
    }

    rows.forEach(item => {
        $list.append(`
            <div class="geo-item">
                <strong>${escapeHtml(item.location)}</strong>
                <span>${formatNumber(item.count)} lượt</span>
            </div>
        `);
    });
}

function emptyRow(colspan) {
    return `<tr><td colspan="${colspan}" class="text-center text-slate-400 py-8">Không có dữ liệu trong kỳ lọc</td></tr>`;
}

function formatMoney(value) {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value || 0);
}

function compactMoney(value) {
    return new Intl.NumberFormat('vi-VN', { notation: 'compact', maximumFractionDigits: 1 }).format(value || 0);
}

function formatNumber(value) {
    return new Intl.NumberFormat('vi-VN').format(value || 0);
}

function formatPercent(value) {
    return `${new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(value || 0)}%`;
}

function escapeHtml(value) {
    return String(value || '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}
