/**
 * 🌸 Notifications JavaScript
 * Handles dropdown toggle and AJAX marking as read
 */

$(document).ready(function () {
    const $wrapper = $('#notiBellWrapper');
    const $btn = $('#notiBellBtn');
    const $dropdown = $('#notiDropdown');

    // Toggle Dropdown - Allow clicking the whole wrapper or just the btn
    $wrapper.on('click', function (e) {
        e.stopPropagation();
        $dropdown.toggleClass('show');
    });

    // Close on click outside
    $(document).on('click', function (e) {
        if (!$dropdown.is(e.target) && $dropdown.has(e.target).length === 0 && !$btn.is(e.target) && $btn.has(e.target).length === 0) {
            $dropdown.removeClass('show');
        }
    });

    // Mark as read when clicking an item
    $('.noti-item.unread').on('click', function (e) {
        const $item = $(this);
        const notiId = $item.data('id');

        $.post('/Account/MarkNotificationRead', { id: notiId }, function (res) {
            if (res.success) {
                $item.removeClass('unread');
                updateBadge(res.unreadCount);
            }
        });
    });

    // Mark all as read
    $('#markAllRead').on('click', function (e) {
        e.preventDefault();
        $.post('/Account/MarkAllNotificationsRead', function (res) {
            if (res.success) {
                $('.noti-item').removeClass('unread');
                updateBadge(0);
                $('#markAllRead').fadeOut();
            }
        });
    });

    function updateBadge(count) {
        const $badge = $('.noti-badge');
        if (count > 0) {
            if ($badge.length) {
                $badge.text(count);
            } else {
                $btn.append(`<span class="noti-badge">${count}</span>`);
            }
        } else {
            $badge.remove();
        }
    }
});
