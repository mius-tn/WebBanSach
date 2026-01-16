// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    // Fix for Mega Menu: When clicking to close, prevent immediate Hover re-opening
    $('.mega-dropdown').on('hide.bs.dropdown', function () {
        var $el = $(this);
        $el.addClass('no-hover');
        setTimeout(function () {
            $el.removeClass('no-hover');
        }, 500); // 500ms cooldown
    });
});
