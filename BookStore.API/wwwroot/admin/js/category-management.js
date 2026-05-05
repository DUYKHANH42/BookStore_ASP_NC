$(document).ready(function () {
    // Category Form Submit
    $('#categoryForm').submit(function (e) {
        e.preventDefault();
        const formData = $(this).serialize();
        
        ValidationHelper.clearErrors($(this));
        
        $.post('/Admin/Category/UpsertCategory', formData, function (res) {
            if (res.success) {
                closeCategoryModal();
                loadCategoryList();
                toastr.success(res.message);
            } else {
                if (res.errors) {
                    ValidationHelper.showErrors($('#categoryForm'), res.errors);
                } else {
                    Swal.fire('Lỗi', res.message, 'error');
                }
            }
        });
    });

    // SubCategory Form Submit
    $('#subCategoryForm').submit(function (e) {
        e.preventDefault();
        const formData = $(this).serialize();
        
        ValidationHelper.clearErrors($(this));
        
        $.post('/Admin/Category/UpsertSubCategory', formData, function (res) {
            if (res.success) {
                closeSubCategoryModal();
                loadCategoryList();
                toastr.success(res.message);
            } else {
                if (res.errors) {
                    ValidationHelper.showErrors($('#subCategoryForm'), res.errors);
                } else {
                    Swal.fire('Lỗi', res.message, 'error');
                }
            }
        });
    });
});

function loadCategoryList() {
    $.get('/Admin/Category/GetCategoryList', function (data) {
        $('#categoryListContainer').html(data);
    });
}

// Modal Category
window.openCategoryModal = function (id) {
    ValidationHelper.clearErrors($('#categoryForm'));
    $('#categoryForm')[0].reset();
    $('#categoryId').val(id);
    
    if (id === 0) {
        $('#categoryModalTitle').text('Thêm danh mục mới');
        $('#categoryModal').addClass('active');
    } else {
        $('#categoryModalTitle').text('Chỉnh sửa danh mục');
        $.get(`/Admin/Category/GetCategory/${id}`, function (data) {
            $('input[name="name"]').val(data.name);
            $('#categoryModal').addClass('active');
        });
    }
}

window.closeCategoryModal = function () {
    $('#categoryModal').removeClass('active');
}

// Modal SubCategory
window.openSubCategoryModal = function (id, categoryId, categoryName) {
    ValidationHelper.clearErrors($('#subCategoryForm'));
    $('#subCategoryForm')[0].reset();
    $('#subCategoryId').val(id);
    $('#parentCategoryId').val(categoryId);
    $('#parentCategoryName').text(categoryName);
    
    if (id === 0) {
        $('#subCategoryModalTitle').text('Thêm danh mục phụ');
        $('#subCategoryModal').addClass('active');
    } else {
        $('#subCategoryModalTitle').text('Chỉnh sửa danh mục phụ');
        $.get(`/Admin/Category/GetSubCategory/${id}`, function (data) {
            $('input[name="name"]').val(data.name);
            $('#subCategoryModal').addClass('active');
        });
    }
}

window.closeSubCategoryModal = function () {
    $('#subCategoryModal').removeClass('active');
}

// Delete Category
window.deleteCategory = function (id) {
    Swal.fire({
        title: 'Xác nhận xóa?',
        text: "Các danh mục phụ liên quan cũng có thể bị ảnh hưởng.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#64748b',
        confirmButtonText: 'Đồng ý xóa',
        cancelButtonText: 'Hủy'
    }).then((result) => {
        if (result.isConfirmed) {
            $.post('/Admin/Category/DeleteCategory', { id: id }, function (res) {
                if (res.success) {
                    loadCategoryList();
                    toastr.success(res.message);
                } else {
                    Swal.fire('Lỗi', res.message, 'error');
                }
            });
        }
    });
}

// Delete SubCategory
window.deleteSubCategory = function (id) {
    Swal.fire({
        title: 'Xác nhận xóa?',
        text: "Bạn có chắc chắn muốn xóa danh mục phụ này?",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#64748b',
        confirmButtonText: 'Đồng ý xóa',
        cancelButtonText: 'Hủy'
    }).then((result) => {
        if (result.isConfirmed) {
            $.post('/Admin/Category/DeleteSubCategory', { id: id }, function (res) {
                if (res.success) {
                    loadCategoryList();
                    toastr.success(res.message);
                } else {
                    Swal.fire('Lỗi', res.message, 'error');
                }
            });
        }
    });
}


