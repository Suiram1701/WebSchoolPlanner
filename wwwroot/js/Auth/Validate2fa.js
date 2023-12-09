"use strict";

$().ready(function () {

    // Send the form if enough letters are given
    let inputElement = $("input[id='codeInput']");
    const requiredLetters = parseInt(inputElement.attr("data-digit-count"));
    inputElement.on("input", function () {
        const content = inputElement.val().replace(" ", "");     // Remove spaces
        if (content.length >= requiredLetters) {
            $("form[id='codeForm']").submit();
        }
    });

    // Remove invalid mark on input
    let form = $("form[id='codeForm']");
    if (!form.hasClass("form-invalid"))     // Use only when input invalid
        return;

    $("input[type='text']").each(function () {
        $(this).on("input", function () {
            form.removeClass("form-invalid");
        });
    });
});