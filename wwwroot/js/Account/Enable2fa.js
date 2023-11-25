"use strict";

$().ready(function () {

    // Send the form if enough letterrs are given
    let inputElement = $("input[id='codeInput']");
    const requiredLetters = parseInt(inputElement.attr("data-digit-count"));
    inputElement.on("input", function () {
        if (inputElement.val().length >= requiredLetters) {
            $("form[id='codeForm']").submit();
        }
    });
});