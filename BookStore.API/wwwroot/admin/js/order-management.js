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

    if (!confirm('Bạn có chắc chắn muốn cập nhật trạng thái đơn hàng này?')) return;

    btn.prop('disabled', true).html('<span class="material-symbols-outlined animate-spin text-sm">refresh</span>');

    $.post('/Admin/Order/UpdateStatus', { id: id, status: status }, function (res) {
        if (res.success) {
            alert(res.message);
            location.reload();
        } else {
            alert(res.message);
            btn.prop('disabled', false).html(originalContent);
        }
    });
});

// Close modal on ESC key
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') closeOrderModal();
});
