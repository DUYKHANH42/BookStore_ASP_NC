function openOrderModal(id) {
    const modal = document.getElementById('orderModal');
    const modalBody = document.getElementById('orderModalBody');

    modalBody.innerHTML = `
        <div class="flex flex-col items-center justify-center py-20">
            <div class="w-12 h-12 border-4 border-blue-600 border-t-transparent rounded-full animate-spin mb-4"></div>
            <p class="font-bold text-slate-400">Đang tải chi tiết...</p>
        </div>
    `;
    modal.classList.add('active');

    $.get('/Admin/Order/GetOrderDetails/' + id, function (data) {
        modalBody.innerHTML = data;
    }).fail(function() {
        modalBody.innerHTML = '<div class="p-10 text-center text-rose-500 font-bold">Lỗi khi tải dữ liệu!</div>';
    });
}

function closeOrderModal() {
    const modal = document.getElementById('orderModal');
    modal.classList.remove('active');
}

$(document).on('click', '.btn-update-status', function () {
    const id = $(this).data('id');
    const status = $(this).data('status');
    const btn = $(this);
    const originalContent = btn.html();

    Swal.fire({
        title: 'Cập nhật trạng thái?',
        text: 'Bạn có chắc chắn muốn cập nhật trạng thái đơn hàng này?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Đồng ý',
        cancelButtonText: 'Hủy',
        customClass: {
            confirmButton: 'px-6 py-3 bg-blue-600 text-white rounded-2xl font-black text-xs uppercase tracking-widest outline-none border-none mx-2',
            cancelButton: 'px-6 py-3 bg-slate-200 text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest outline-none border-none mx-2'
        },
        buttonsStyling: false
    }).then((result) => {
        if (!result.isConfirmed) return;

        btn.prop('disabled', true).html('<span class="material-symbols-outlined animate-spin text-sm">refresh</span>');

        $.post('/Admin/Order/UpdateStatus', { id: id, status: status }, function (res) {
            if (res.success) {
                toastr.success(res.message);
                setTimeout(() => location.reload(), 800);
            } else {
                toastr.error(res.message || 'Không thể cập nhật trạng thái');
                btn.prop('disabled', false).html(originalContent);
            }
        });
    });
});

// Close modal on ESC key
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') closeOrderModal();
});

$(document).ready(function() {
    const urlParams = new URLSearchParams(window.location.search);
    const orderId = urlParams.get('orderId');
    if (orderId) {
        // Đợi một chút để đảm bảo trang đã load xong các thành phần cần thiết
        setTimeout(() => {
            openOrderModal(orderId);
        }, 500);
    }
});
