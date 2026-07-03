function setActiveMenu(element) {
    document.querySelectorAll('.nav-link').forEach(function (navLink) {
        navLink.classList.remove('active');
    });

    element.classList.add('active');

    let parentElement = element;
    while (parentElement) {
        if (parentElement.classList.contains('collapse')) {
            parentElement.classList.add('show');
            const menuLink = parentElement.previousElementSibling;
            if (menuLink && menuLink.classList.contains('menu-link')) {
                menuLink.setAttribute('aria-expanded', 'true');
            }
        }
        parentElement = parentElement.parentElement;
    }
}

document.addEventListener('DOMContentLoaded', function () {
    const currentUrl = window.location.href;

    document.querySelectorAll('.nav-link').forEach(function (navLink) {
        if (navLink.href === currentUrl) {
            setActiveMenu(navLink);
        }
    });
});


/*generateGradient start*/
function updateColor(input) {
    input.style.background = input.value;
}

function generateGradient() {
    const color1 = document.getElementById('color1').value;
    const color2 = document.getElementById('color2').value;
    const color3 = document.getElementById('color3').value;
    let angle = document.getElementById('angle').value;
    const disableColor2 = document.getElementById('disableColor2').checked;
    const disableColor3 = document.getElementById('disableColor3').checked;

    // Ensure angle is within valid range
    angle = Math.max(-360, Math.min(360, angle));

    let gradient;
    if (disableColor2) {
        gradient = color1; // Solid color if only one color is selected
    } else if (disableColor3) {
        gradient = `linear-gradient(${angle}deg, ${color1}, ${color2})`;
    } else {
        gradient = `linear-gradient(${angle}deg, ${color1}, ${color2}, ${color3})`;
    }

    // Apply gradient to the preview box inside modal
    document.getElementById('gradient-box').style.background = gradient;

    // Display generated CSS code in the modal
    document.getElementById('css-code').textContent = `background: ${gradient};`;

    // Update the input field outside the modal with the gradient code
    document.getElementById('gradientInput').value = `background: ${gradient};`;
}

// Initialize the background colors of input fields
document.querySelectorAll('.color-input').forEach(input => updateColor(input));