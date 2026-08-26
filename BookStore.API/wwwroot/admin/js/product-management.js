/**
 * Product Management AJAX & UI Logic
 */

let currentPage = 1;

function getValidationHelper() {
    return window.ValidationHelper;
}

function getLumenAlert() {
    return window.LumenAlert;
}

function setSubCategoryLoading($select, text) {
    $select.prop('disabled', true).html(`<option value="">${text}</option>`);
}

function normalizeSubCategories(items) {
    return (Array.isArray(items) ? items : []).map(item => ({
        id: item.id ?? item.Id,
        name: item.name ?? item.Name,
        categoryId: item.categoryId ?? item.CategoryId
    })).filter(item => item.id && item.name);
}

function loadSubCategoriesFor(targetSelect, categoryId, selectedId) {
    const $target = $(targetSelect);
    if ($target.length === 0) return $.Deferred().resolve().promise();

    if (!categoryId) {
        const placeholder = targetSelect.includes('form') ? 'Chọn danh mục chính trước' : 'Tất cả';
        $target.html(`<option value="">${placeholder}</option>`).prop('disabled', true);
        return $.Deferred().resolve().promise();
    }

    $target.prop('disabled', true).html('<option value="">Đang tải...</option>');

    return $.get('/Admin/Category/GetSubCategories', { categoryId: categoryId })
        .done(function (data) {
            let html = targetSelect.includes('form') ? '<option value="">Chọn danh mục phụ</option>' : '<option value="">Tất cả</option>';

            if (data && Array.isArray(data)) {
                data.forEach(item => {
                    const id = item.id ?? item.Id;
                    const name = item.name ?? item.Name;
                    const isSelected = selectedId && String(id) === String(selectedId) ? 'selected' : '';
                    html += `<option value="${id}" ${isSelected}>${name}</option>`;
                });
            }

            // Đổ HTML vào thẻ thật - Vì có class 'no-choices' nên nó sẽ hiện ra ngay lập tức
            $target.html(html).prop('disabled', false);

            if (selectedId) {
                $target.val(String(selectedId));
            }

            console.log("==> ĐÃ HIỆN DỮ LIỆU VÀO " + targetSelect);
        })
        .fail(function () {
            $target.html('<option value="">Lỗi tải dữ liệu</option>');
        });
}


$(document).ready(function () {
    // Initial load
    loadProducts();
    setSubCategoryLoading($('#subCategoryId'), '');
    setSubCategoryLoading($('#formSubCategoryId'), '');

    $('#categoryId').off('change').on('change', function () {
        const catId = $(this).val();
        loadSubCategoriesFor('#subCategoryId', catId, null).always(() => {
            loadProducts(1);
        });
    });

    $('#subCategoryId, #sortBy, #isActive').off('change').on('change', function () {
        loadProducts(1);
    });
    // C. Xử lý TRONG MODAL (Form)
    $(document).off('change', '#formCategoryId').on('change', '#formCategoryId', function () {
        const catId = $(this).val();
        loadSubCategoriesFor('#formSubCategoryId', catId, null);
    });

    // D. Tìm kiếm & Reset
    $('#searchTerm').on('input', function () {
        clearTimeout(this.delay);
        this.delay = setTimeout(() => loadProducts(1), 500);
    });

    $('#resetFilters').on('click', function () {
        $('#searchTerm').val('');
        $('#categoryId').val('');
        $('#subCategoryId').html('<option value="">Tất cả</option>').prop('disabled', true);
        loadProducts(1);
    });


    // Pagination delegation
    $(document).on('click', '.pagination-link', function (e) {
        e.preventDefault();
        const page = $(this).data('page');
        loadProducts(page);
    });

    // Image Selection Area click
    $(document).on('click', '#imageSelectArea, .select-image-btn', function (e) {
        if (e.target.id !== 'productImage') {
            $('#productImage').click();
        }
    });

    // Image Preview logic
    $('#productImage').change(function () {
        const file = this.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                $('#imagePreview').html(`<img src="${e.target.result}" class="w-full h-full object-cover">`);
            };
            reader.readAsDataURL(file);
        }
    });

    $('#additionalImages').change(function () {
        const files = this.files;
        $('#galleryPreview').html(''); // Clear previous previews
        if (files && files.length > 0) {
            for (let i = 0; i < files.length; i++) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    $('#galleryPreview').append(`<div class="w-16 h-16 rounded-xl overflow-hidden shadow-sm shrink-0"><img src="${e.target.result}" class="w-full h-full object-cover"></div>`);
                };
                reader.readAsDataURL(files[i]);
            }
        } else {
            $('#galleryPreview').html('<span class="material-symbols-outlined text-slate-200 text-4xl mt-6">collections</span>');
        }
    });

    // Form Submit logic
    $('#productForm').submit(function (e) {
        e.preventDefault();

        const VH = getValidationHelper();
        if (!VH || typeof VH.validateForm !== 'function') {
            console.error('ValidationHelper not loaded.');
            return;
        }
        if (!VH.validateForm($(this))) {
            return;
        }

        const formData = new FormData(this);
        const submitBtn = $(this).find('button[type="submit"]');
        const originalBtnText = submitBtn.html();

        // Disable button & show loading
        submitBtn.prop('disabled', true).html('<span class="flex items-center gap-2"><div class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div> Đang lưu...</span>');

        $.ajax({
            url: '/Admin/Product/Upsert',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (res) {
                if (res.success) {
                    toastr.success(res.message);
                    closeProductModal();
                    loadProducts(currentPage);
                } else {
                    if (res.errors) {
                        const helper = getValidationHelper();
                        if (helper) helper.showErrors($('#productForm'), res.errors);
                    } else {
                        toastr.error(res.message || 'Không thể lưu sản phẩm');
                    }
                }
            },
            error: function () {
                toastr.error('Có lỗi hệ thống xảy ra trong quá trình xử lý.');
            },
            complete: function () {
                // Restore button
                submitBtn.prop('disabled', false).html(originalBtnText);
            }
        });
    });

    $(document).on('change', '#formCategoryId', function () {
        $('#formSubCategoryId').val('');
        loadSubCategoriesFor('#formSubCategoryId', $(this).val(), null);
    });


});

/**
 * Loads products via AJAX
 * @param {number} page 
 */
function loadProducts(page = 1) {
    currentPage = page;
    const filters = {
        search: $('#searchTerm').val(),
        categoryId: $('#categoryId').val(),
        subCategoryId: $('#subCategoryId').val(),
        sortBy: $('#sortBy').val(),
        isActive: $('#isActive').val(),
        pageNumber: page,
        pageSize: 10
    };

    $('#productListContainer').addClass('opacity-50 pointer-events-none');

    $.get('/Admin/Product/GetProductList', filters, function (data) {
        $('#productListContainer').html(data).removeClass('opacity-50 pointer-events-none');
    });
}

/**
 * Modal Controls
 */
window.openProductModal = function (id) {
    const VH = getValidationHelper();
    if (VH) VH.clearErrors($('#productForm'));
    $('#productForm')[0].reset();
    $('#productId').val(id);
    $('#imagePreview').html('<span class="material-symbols-outlined notranslate text-slate-200 text-5xl" translate="no">add_photo_alternate</span>');
    $('#galleryPreview').html('<span class="material-symbols-outlined notranslate text-slate-200 text-4xl mt-6" translate="no">collections</span>');
    $('#existingImagesArea').addClass('hidden');
    $('#existingImagesContainer').empty();

    if (id === 0) {
        $('#modalTitle').text('Thêm sản phẩm mới');
        // New product: quantity is 0 and readonly (managed via inventory imports)
        const $qty = $('input[name="quantity"]');
        $qty.val(0).prop('readonly', true).attr('title', 'Số lượng được quản lý qua Phiếu nhập kho');
        setSubCategoryLoading($('#formSubCategoryId'), 'Chọn danh mục chính trước');
        $('#productModal').addClass('active');
    } else {
        $('#modalTitle').text('Chỉnh sửa sản phẩm');
        $.get(`/Admin/Product/GetProduct/${id}`, function (data) {
            $('input[name="name"]').val(data.name);
            $('input[name="brand"]').val(data.brand);
            $('input[name="sku"]').val(data.sku);
            $('input[name="price"]').val(data.price);
            // When editing: quantity is readonly — stock managed via imports
            const $qty = $('input[name="quantity"]');
            $qty.val(data.quantity).prop('readonly', true).attr('title', 'Số lượng được quản lý qua Phiếu nhập kho');

            $('select[name="categoryId"]').val(data.categoryId);
            loadSubCategoriesFor('#formSubCategoryId', data.categoryId, data.subCategoryId);
            $('textarea[name="description"]').val(data.description);

            if (data.imageUrl) {
                const url = data.imageUrl.startsWith('http') ? data.imageUrl : '/uploads/' + data.imageUrl;
                $('#imagePreview').html(`<img src="${url}" class="w-full h-full object-cover">`);
            }

            // Xử lý bộ sưu tập ảnh (Images)
            if (data.images && data.images.length > 0) {
                const additionalImages = data.images.filter(img => !img.isMain);
                if (additionalImages.length > 0) {
                    $('#existingImagesArea').removeClass('hidden');
                    additionalImages.forEach(img => {
                        const imgUrl = img.imageUrl.startsWith('http') ? img.imageUrl : '/uploads/' + img.imageUrl;
                        $('#existingImagesContainer').append(`
                            <div class="relative group w-16 h-16 rounded-xl overflow-hidden shadow-sm border border-slate-200" id="img-container-${img.id}">
                                <img src="${imgUrl}" class="w-full h-full object-cover">
                                <button type="button" onclick="deleteProductImage(${img.id})" 
                                        class="absolute inset-0 bg-rose-600/80 text-white opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
                                    <span class="material-symbols-outlined text-sm">close</span>
                                </button>
                            </div>
                        `);
                    });
                }
            }

            $('#productModal').addClass('active');
        });
    }
};

window.deleteProductImage = function (imageId) {
    const alert = getLumenAlert();
    const confirm = alert.danger({
        title: 'Xóa ảnh này?',
        text: "Ảnh sẽ bị xóa vĩnh viễn khỏi hệ thống.",
        confirmButtonText: 'Xóa ngay',
        cancelButtonText: 'Hủy'
    });

    confirm.then((result) => {
        if (result.isConfirmed) {
            $.post('/Admin/Product/DeleteImage', { imageId: imageId }, function (res) {
                if (res.success) {
                    $(`#img-container-${imageId}`).fadeOut(300, function () {
                        $(this).remove();
                        if ($('#existingImagesContainer').children().length === 0) {
                            $('#existingImagesArea').addClass('hidden');
                        }
                    });
                    toastr.success(res.message);
                } else {
                    alert.error('Lỗi', res.message);
                }
            });
        }
    });
};

window.closeProductModal = function () {
    $('#productModal').removeClass('active');
};

window.editProduct = function (id) {
    openProductModal(id);
};

window.toggleProductStatus = function (id) {
    const alert = getLumenAlert();
    const confirm = alert.confirm({
        title: 'Xác nhận?',
        text: "Bạn có chắc chắn muốn thay đổi trạng thái sản phẩm này?",
        confirmButtonText: 'Đồng ý',
        cancelButtonText: 'Hủy'
    });

    confirm.then((result) => {
        if (result.isConfirmed) {
            $.post('/Admin/Product/ToggleStatus', { id: id }, function (res) {
                if (res.success) {
                    loadProducts(currentPage);
                    toastr.success('Cập nhật trạng thái thành công');
                }
            });
        }
    });
};