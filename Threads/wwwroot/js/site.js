document.addEventListener("DOMContentLoaded", function () {
    var currentUrl = window.location.href;

    // Loop through each navigation link within the specified div and check if it matches the current URL
    var navLinks = document.querySelectorAll(".navbar-collapse .nav-link");
    navLinks.forEach(function (link) {
        if (link.href === currentUrl) {
            link.classList.add("active");
            var icon = link.querySelector("i");
            if (icon) {
                var iconClassList = icon.classList;
                var oldClassName = iconClassList[1]; // Assuming the class is always in the second position
                var newClassName = oldClassName + "-fill";

                if (oldClassName !== "bi-pencil-square") {
                    iconClassList.remove(oldClassName);
                    iconClassList.add(newClassName);
                }
            }
        }
    });
});