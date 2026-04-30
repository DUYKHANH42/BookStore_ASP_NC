/**
 * Product Management AJAX & UI Logic
 */

let currentPage = 1;

$(document).ready(function () {
    // Initial load
    loadProducts();

    // Event Listeners for Filters
    $('#searchTerm').on('input', function () {
        clearTimeout(this.delay);
        this.delay = setTimeout(() => loadProducts(1), 500);
    });

    $('#categoryId, #subCategoryId, #sortBy, #isActive').change(function () {
        loadProducts(1);
    });

    $('#resetFilters').click(function () {
        $('#searchTerm').val('');
        $('#categoryId').val('');
        $('#subCategoryId').val('');
        $('#sortBy').val('newest');
        $('#isActive').val('true');
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

        if (!this.checkValidity()) {
            this.reportValidity();
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
                    alert(res.message);
                    closeProductModal();
                    loadProducts(currentPage);
                } else {
                    alert(res.message);
                }
            },
            error: function () {
                alert('Có lỗi xảy ra trong quá trình xử lý.');
            },
            complete: function () {
                // Restore button
                submitBtn.prop('disabled', false).html(originalBtnText);
            }
        });
    });

    // Dynamic SubCategory Filtering based on Category
    $('#categoryId, #formCategoryId').change(function () {
        const catId = $(this).val();
        const targetSelect = $(this).attr('id') === 'categoryId' ? '#subCategoryId' : '#formSubCategoryId';

        if (!catId) {
            $(targetSelect + ' option').show();
        } else {
            $(targetSelect + ' option').hide();
            $(targetSelect + ' option[value=""]').show();
            $(`${targetSelect} option[data-category="${catId}"]`).show();
        }
    });

    // Flash Sale Form Submit
    $('#flashSaleForm').submit(function (e) {
        e.preventDefault();
        const formData = $(this).serialize();
        const productId = $('#fsProductId').val();

        $.post('/Admin/FlashSale/Create', formData, function (res) {
            if (res.success) {
                alert(res.message);
                loadFlashSales(productId);
                $('#flashSaleForm')[0].reset();
            } else {
                alert(res.message);
            }
        });
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
    $('#productForm')[0].reset();
    $('#productId').val(id);
    $('#imagePreview').html('<span class="material-symbols-outlined text-slate-200 text-5xl">add_photo_alternate</span>');
    $('#galleryPreview').html('<span class="material-symbols-outlined text-slate-200 text-4xl mt-6">collections</span>');

    if (id === 0) {
        $('#modalTitle').text('Thêm sản phẩm mới');
        $('#productModal').addClass('active');
    } else {
        $('#modalTitle').text('Chỉnh sửa sản phẩm');
        $.get(`/Admin/Product/GetProduct/${id}`, function (data) {
            $('input[name="name"]').val(data.name);
            $('input[name="brand"]').val(data.brand);
            $('input[name="sku"]').val(data.sku);
            $('input[name="price"]').val(data.price);
            $('input[name="quantity"]').val(data.quantity);

            $('select[name="categoryId"]').val(data.categoryId).trigger('change');
            setTimeout(() => $('select[name="subCategoryId"]').val(data.subCategoryId), 100);
            $('textarea[name="description"]').val(data.description);

            if (data.imageUrl) {
                const url = data.imageUrl.startsWith('http') ? data.imageUrl : '/uploads/' + data.imageUrl;
                $('#imagePreview').html(`<img src="${url}" class="w-full h-full object-cover">`);
            }
            
            // Xử lý bộ sưu tập ảnh (Images)
            if (data.images && data.images.length > 0) {
                $('#galleryPreview').html('');
                const additionalImages = data.images.filter(img => !img.isMain); // Lọc ra ảnh phụ
                if (additionalImages.length > 0) {
                    additionalImages.forEach(img => {
                        const imgUrl = img.imageUrl.startsWith('http') ? img.imageUrl : '/uploads/' + img.imageUrl;
                        $('#galleryPreview').append(`<div class="w-16 h-16 rounded-xl overflow-hidden shadow-sm shrink-0"><img src="${imgUrl}" class="w-full h-full object-cover"></div>`);
                    });
                } else {
                    $('#galleryPreview').html('<span class="material-symbols-outlined text-slate-200 text-4xl mt-6">collections</span>');
                }
            } else {
                $('#galleryPreview').html('<span class="material-symbols-outlined text-slate-200 text-4xl mt-6">collections</span>');
            }

            $('#productModal').addClass('active');
        });
    }
};

window.closeProductModal = function () {
    $('#productModal').removeClass('active');
};

window.editProduct = function (id) {
    openProductModal(id);
};

window.toggleProductStatus = function (id) {
    if (!confirm('Bạn có chắc chắn muốn thay đổi trạng thái sản phẩm này?')) return;

    $.post('/Admin/Product/ToggleStatus', { id: id }, function (res) {
        if (res.success) {
            loadProducts(currentPage);
        }
    });
};

/**
 * Flash Sale Management Logic
 */

window.manageFlashSale = function (id, name) {
    $('#fsProductId').val(id);
    $('#fsProductName').text('Sản phẩm: ' + name);
    $('#flashSaleForm')[0].reset();
    $('#flashSaleModal').removeClass('hidden');
    loadFlashSales(id);
};

window.closeFlashSaleModal = function () {
    $('#flashSaleModal').addClass('hidden');
};

function loadFlashSales(productId) {
    $('#flashSaleListContainer').html('<div class="flex justify-center py-10"><div class="w-8 h-8 border-2 border-rose-500 border-t-transparent rounded-full animate-spin"></div></div>');

    $.get('/Admin/FlashSale/GetSales', { productId: productId }, function (data) {
        $('#flashSaleListContainer').html(data);
    });
}

window.toggleSaleStatus = function (id) {
    $.post('/Admin/FlashSale/ToggleStatus', { id: id }, function (res) {
        if (res.success) {
            loadFlashSales($('#fsProductId').val());
        }
    });
};

window.deleteSale = function (id) {
    if (!confirm('Bạn có chắc chắn muốn xóa chương trình sale này?')) return;

    $.post('/Admin/FlashSale/Delete', { id: id }, function (res) {
        if (res.success) {
            loadFlashSales($('#fsProductId').val());
        } else {
            alert(res.message);
        }
    });
};
