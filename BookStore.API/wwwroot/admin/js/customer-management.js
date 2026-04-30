function openCustomerModal(id) {
    const modal = document.getElementById('customerModal');
    const modalBody = document.getElementById('customerModalBody');

    modalBody.innerHTML = `
        <div class="flex flex-col items-center justify-center py-20">
            <div class="w-12 h-12 border-4 border-blue-600 border-t-transparent rounded-full animate-spin mb-4"></div>
            <p class="font-bold text-slate-400">Đang tải dữ liệu khách hàng...</p>
        </div>
    `;
    modal.classList.add('active');

    $.get('/Admin/Customer/GetCustomerDetails/' + id, function (data) {
        modalBody.innerHTML = data;
    }).fail(function() {
        modalBody.innerHTML = '<div class="p-10 text-center text-rose-500 font-bold">Lỗi khi tải dữ liệu!</div>';
    });
}

function closeCustomerModal() {
    const modal = document.getElementById('customerModal');
    modal.classList.remove('active');
}

$(document).on('submit', '#resetPasswordForm', function (e) {
    e.preventDefault();
    const id = $('#customerId').val();
    const newPassword = $('#newPassword').val();
    const btn = $(this).find('button[type="submit"]');

    if (newPassword.length < 6) {
        alert('Mật khẩu phải ít nhất 6 ký tự');
        return;
    }

    btn.prop('disabled', true).html('Đang xử lý...');

    $.post('/Admin/Customer/ResetPassword', { id: id, newPassword: newPassword }, function (res) {
        if (res.success) {
            alert(res.message);
            closeCustomerModal();
        } else {
            alert(res.message);
            btn.prop('disabled', false).html('Cập nhật mật khẩu');
        }
    });
});

$(document).on('click', '.btn-toggle-status', function () {
    const id = $(this).data('id');
    const isActive = $(this).data('active');
    const message = isActive ? "Bạn có chắc muốn KHÓA tài khoản này?" : "Bạn có chắc muốn MỞ KHÓA tài khoản này?";

    if (!confirm(message)) return;

    $.post('/Admin/Customer/UpdateStatus', { id: id, isActive: !isActive }, function (res) {
        if (res.success) {
            alert(res.message);
            location.reload();
        } else {
            alert(res.message);
        }
    });
});

document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') closeCustomerModal();
});
