$(document).ready(function () {
    // Category Form Submit
    $('#categoryForm').submit(function (e) {
        e.preventDefault();
        const formData = $(this).serialize();
        
        $.post('/Admin/Category/UpsertCategory', formData, function (res) {
            if (res.success) {
                closeCategoryModal();
                loadCategoryList();
                showToast(res.message, 'success');
            }
        });
    });

    // SubCategory Form Submit
    $('#subCategoryForm').submit(function (e) {
        e.preventDefault();
        const formData = $(this).serialize();
        
        $.post('/Admin/Category/UpsertSubCategory', formData, function (res) {
            if (res.success) {
                closeSubCategoryModal();
                loadCategoryList();
                showToast(res.message, 'success');
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
    if (confirm('Bạn có chắc chắn muốn xóa danh mục này? Các danh mục phụ liên quan cũng có thể bị ảnh hưởng.')) {
        $.post('/Admin/Category/DeleteCategory', { id: id }, function (res) {
            if (res.success) {
                loadCategoryList();
                showToast(res.message, 'success');
            } else {
                showToast(res.message, 'error');
            }
        });
    }
}

// Delete SubCategory
window.deleteSubCategory = function (id) {
    if (confirm('Bạn có chắc chắn muốn xóa danh mục phụ này?')) {
        $.post('/Admin/Category/DeleteSubCategory', { id: id }, function (res) {
            if (res.success) {
                loadCategoryList();
                showToast(res.message, 'success');
            } else {
                showToast(res.message, 'error');
            }
        });
    }
}

function showToast(message, type) {
    // Giả sử có toast component, nếu chưa có thì dùng alert tạm
    alert(message);
}
