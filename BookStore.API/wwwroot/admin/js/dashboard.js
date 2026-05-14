let revenueChart, statusChart;

$(function () {
    if ($('#revenueChart').length) {
        initCharts(initialRevenueLabels, initialRevenueData, initialOrderData, initialStatusLabels, initialStatusData);
    }

    $('#btnFilter').on('click', function () {
        loadDashboardData();
    });
});

function initCharts(revLabels, revData, ordData, stLabels, stData) {
    // Hủy biểu đồ cũ nếu tồn tại để tránh lỗi chồng lấn
    if (revenueChart) revenueChart.destroy();
    if (statusChart) statusChart.destroy();

    const revCtx = $('#revenueChart')[0].getContext('2d');
    revenueChart = new Chart(revCtx, {
        type: 'line',
        data: {
            labels: revLabels,
            datasets: [
                {
                    label: 'Doanh thu',
                    data: revData,
                    borderColor: '#3b82f6',
                    backgroundColor: 'rgba(59, 130, 246, 0.1)',
                    fill: true,
                    tension: 0.4,
                    borderWidth: 4,
                    pointBackgroundColor: '#fff',
                    pointBorderColor: '#3b82f6',
                    pointBorderWidth: 2,
                    yAxisID: 'y'
                },
                {
                    label: 'Số đơn hàng',
                    data: ordData,
                    borderColor: '#10b981',
                    backgroundColor: 'transparent',
                    borderDash: [5, 5],
                    tension: 0.4,
                    borderWidth: 2,
                    yAxisID: 'y1'
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'index', intersect: false },
            plugins: { 
                legend: { 
                    position: 'top',
                    align: 'end',
                    labels: { font: { weight: 'bold', size: 11 } }
                }
            },
            scales: {
                y: {
                    type: 'linear',
                    display: true,
                    position: 'left',
                    grid: { color: 'rgba(0,0,0,0.05)' },
                    ticks: { callback: (v) => v.toLocaleString('vi-VN') + 'đ' }
                },
                y1: {
                    type: 'linear',
                    display: true,
                    position: 'right',
                    grid: { drawOnChartArea: false },
                    ticks: { font: { weight: 'bold' } }
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
            labels: stLabels,
            datasets: [{
                data: stData,
                backgroundColor: [
                    '#1e3a8a', // Xanh dương đậm nhất
                    '#2563eb', 
                    '#3b82f6', 
                    '#60a5fa', 
                    '#93c5fd'  // Xanh dương nhạt nhất
                ],
                borderWidth: 0,
                hoverOffset: 20
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '80%',
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
    const ts = new Date().getTime(); // Anti-cache timestamp

    $btn.text('Đang tải...').prop('disabled', true);

    // 1. Lấy HTML thống kê
    $.get('/Admin/Home/GetDashboardPartial', { from: start, to: end, v: ts }, function (html) {
        console.log("HTML received, updating dashboardContent...");
        $('#dashboardContent').html(html);

        // 2. Lấy JSON để vẽ lại biểu đồ ngay sau khi HTML nạp xong
        $.get('/Admin/Home/GetDashboardData', { from: start, to: end, v: ts }, function (res) {
            console.log("Data received, updating charts...");
            const revLabels = res.revenueChart.map(x => x.date);
            const revData = res.revenueChart.map(x => x.revenue);
            const ordData = res.revenueChart.map(x => x.orderCount);
            const stLabels = res.orderStatusChart.map(x => x.status);
            const stData = res.orderStatusChart.map(x => x.count);

            initCharts(revLabels, revData, ordData, stLabels, stData);
        });
    }).fail(function () {
        alert('Không thể tải dữ liệu thống kê.');
    }).always(function () {
        $btn.text(originalText).prop('disabled', false);
    });
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
