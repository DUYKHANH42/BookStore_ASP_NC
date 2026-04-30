let revenueChart, statusChart;

$(function () {
    initCharts();

    $('#btnFilter').on('click', function () {
        loadDashboardData();
    });
});

function initCharts() {
    const revCtx = $('#revenueChart')[0].getContext('2d');
    
    revenueChart = new Chart(revCtx, {
        type: 'line',
        data: {
            labels: initialRevenueLabels,
            datasets: [{
                label: 'Doanh thu',
                data: initialRevenueData,
                borderColor: '#3b82f6',
                backgroundColor: 'rgba(59, 130, 246, 0.1)',
                fill: true,
                tension: 0.4,
                borderWidth: 4,
                pointBackgroundColor: '#fff',
                pointBorderColor: '#3b82f6',
                pointBorderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: { color: 'rgba(0,0,0,0.05)' },
                    ticks: { 
                        font: { weight: 'bold' },
                        callback: (value) => value.toLocaleString('vi-VN') + 'đ'
                    }
                },
                x: {
                    grid: { display: false },
                    ticks: { font: { weight: 'bold' } }
                }
            }
        }
    });

    const statusCtx = $('#statusChart')[0].getContext('2d');
    statusChart = new Chart(statusCtx, {
        type: 'doughnut',
        data: {
            labels: initialStatusLabels,
            datasets: [{
                data: initialStatusData,
                backgroundColor: ['#1e3a8a', '#2563eb', '#3b82f6', '#60a5fa', '#93c5fd'],
                borderWidth: 0,
                hoverOffset: 20
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '70%',
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: { padding: 20, font: { weight: 'bold', size: 11 } }
                }
            }
        }
    });
}

function loadDashboardData() {
    const start = $('#startDate').val();
    const end = $('#endDate').val();
    const $btn = $('#btnFilter');
    const originalText = $btn.text();

    $btn.text('Đang tải...').prop('disabled', true);

    $.get('/Admin/Home/GetDashboardData', { from: start, to: end })
        .done(function (data) {
            updateUI(data);
        })
        .fail(function () {
            alert('Không thể tải dữ liệu thống kê.');
        })
        .always(function () {
            $btn.text(originalText).prop('disabled', false);
        });
}

function updateUI(data) {
    // Cập nhật thẻ thống kê
    $('#statRevenue').text(data.summary.totalRevenue.toLocaleString('vi-VN') + 'đ');
    $('#statOrders').text(data.summary.totalOrders.toLocaleString('vi-VN'));
    $('#statCustomers').text(data.summary.totalCustomers.toLocaleString('vi-VN'));
    $('#statBooks').text(data.summary.totalProducts.toLocaleString('vi-VN')); // Lưu ý: Dùng totalProducts từ DTO mới

    // Cập nhật biểu đồ
    revenueChart.data.labels = $.map(data.revenueChart, (x) => x.date);
    revenueChart.data.datasets[0].data = $.map(data.revenueChart, (x) => x.revenue);
    revenueChart.update();

    statusChart.data.labels = $.map(data.orderStatusChart, (x) => x.status);
    statusChart.data.datasets[0].data = $.map(data.orderStatusChart, (x) => x.count);
    statusChart.update();

    // Cập nhật bảng Top sản phẩm bán chạy
    const $tbody = $('#topSellingTable').empty();
    $.each(data.topSellingProducts, function (i, product) {
        const row = $('<tr>').addClass('group');
        row.append(`<td class="px-6 py-5 bg-slate-50/50 rounded-l-[1.5rem] group-hover:bg-slate-100 transition-premium font-black text-slate-900">${product.name}</td>`);
        row.append(`<td class="px-6 py-5 bg-slate-50/50 group-hover:bg-slate-100 transition-premium font-bold text-slate-600">${product.soldCount}</td>`);
        row.append(`<td class="px-6 py-5 bg-slate-50/50 rounded-r-[1.5rem] group-hover:bg-slate-100 transition-premium text-right font-black text-blue-600">${product.revenue.toLocaleString('vi-VN')}đ</td>`);
        $tbody.append(row);
    });

    // Cập nhật danh sách tồn kho thấp
    const $lowStockDiv = $('#lowStockList').empty();
    if (data.lowStockProducts.length === 0) {
        $lowStockDiv.html('<p class="text-xs text-slate-500 italic">Hiện không có cảnh báo nào.</p>');
    } else {
        $.each(data.lowStockProducts.slice(0, 5), function (i, product) {
            const item = $(`
                <div class="flex items-center gap-5 group cursor-pointer">
                    <div class="w-12 h-16 bg-white/5 rounded-xl overflow-hidden flex-shrink-0 border border-white/5 group-hover:border-blue-500/50 transition-premium">
                        <img src="${product.imageUrl}" alt="${product.name}" class="w-full h-full object-cover opacity-60 group-hover:opacity-100 transition-premium" />
                    </div>
                    <div class="flex-1 min-w-0">
                        <h4 class="text-xs font-black text-white group-hover:text-blue-400 transition-colors truncate">${product.name}</h4>
                        <p class="text-[9px] font-bold text-slate-500 uppercase tracking-widest mt-1">Còn lại: ${product.quantity}</p>
                        <div class="w-full bg-white/5 h-1 rounded-full mt-2 overflow-hidden">
                            <div class="h-full bg-rose-500" style="width: ${Math.min(100, product.quantity * 10)}%"></div>
                        </div>
                    </div>
                </div>
            `);
            $lowStockDiv.append(item);
        });
    }
}

function setQuickFilter(days) {
    const end = new Date();
    let start = new Date();
    
    if (days === 'month') {
        start = new Date(end.getFullYear(), end.getMonth(), 1);
    } else {
        start.setDate(end.getDate() - days);
    }

    $('#startDate').val(start.toISOString().split('T')[0]);
    $('#endDate').val(end.toISOString().split('T')[0]);
    loadDashboardData();
}
