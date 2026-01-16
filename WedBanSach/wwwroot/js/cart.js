$(document).ready(function () {
    // Intercept Add to Cart forms
    $('form[action="/Cart/AddToCart"]').on('submit', function (e) {
        e.preventDefault();
        var form = $(this);
        var btn = form.find('button[type="submit"]');
        var cartIcon = $('.bi-cart3').first();

        // If it's "Buy Now" (has name="buyNow"), let it submit normally
        var clickedBtn = $(document.activeElement);
        if (clickedBtn.attr('name') === 'buyNow') {
            form.off('submit').submit();
            return;
        }

        // Flying Animation
        if (cartIcon.length > 0) {
            var btnOffset = btn.offset();
            var cartOffset = cartIcon.offset();
            var flyingImg = $('<div class="flying-img"></div>');

            // Try to find the product image
            var productImg = btn.closest('.book-card').find('img').attr('src');
            if (!productImg) productImg = $('.book-detail-img').attr('src'); // For details page
            if (!productImg) productImg = ''; // Fallback

            if (productImg) {
                flyingImg.css({
                    'background-image': 'url(' + productImg + ')',
                    'background-size': 'cover'
                });
            } else {
                flyingImg.css('background-color', '#C92127');
            }

            $('body').append(flyingImg);

            flyingImg.css({
                'position': 'absolute',
                'top': btnOffset.top + 'px',
                'left': btnOffset.left + 'px',
                'width': '50px',
                'height': '50px',
                'border-radius': '50%',
                'z-index': '9999',
                'opacity': '1',
                'transition': 'all 0.8s ease-in-out'
            });

            setTimeout(function () {
                flyingImg.css({
                    'top': cartOffset.top + 'px',
                    'left': cartOffset.left + 'px',
                    'width': '20px',
                    'height': '20px',
                    'opacity': '0'
                });
            }, 10);

            setTimeout(function () {
                flyingImg.remove();

                // Perform AJAX Request
                $.post(form.attr('action'), form.serialize(), function (response) {
                    // Update Cart Badge
                    if (response.cartCount !== undefined) {
                        var badge = $('.bi-cart3').parent().find('.badge');
                        if (badge.length > 0) {
                            badge.text(response.cartCount);
                        } else {
                            $('.bi-cart3').parent().append('<span class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger" style="font-size: 10px;">' + response.cartCount + '</span>');
                        }
                    } else {
                        // Fallback if server doesn't return JSON yet (will fix Controller next)
                        location.reload();
                    }
                }).fail(function () {
                    alert("Có lỗi xảy ra khi thêm vào giỏ hàng.");
                });

            }, 800);
        } else {
            // Fallback if no icon found
            form.off('submit').submit();
        }
    });
});
