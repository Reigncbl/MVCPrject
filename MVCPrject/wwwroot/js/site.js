document.addEventListener('DOMContentLoaded', initChatbot);

function initChatbot() {
    // Panel elements
    const addPanel = document.getElementById('add-panel');
    const botPanel = document.getElementById('bot-panel');

    // Button elements
    const addPanelBtn = document.getElementById('add-panel-btn');
    const chatbotOpenBtn = document.getElementById('chatbot-open-btn');
    const addPanelClose = document.getElementById('add-panel-close');

    // Function to close all panels
    function closeAllPanels() {
        addPanel.classList.remove('show');
        botPanel.classList.remove('show');
    }

    // Function to toggle panel
    function togglePanel(panel) {
        const isCurrentlyOpen = panel.classList.contains('show');
        closeAllPanels();
        if (!isCurrentlyOpen) {
            panel.classList.add('show');
        }
    }

    // Event listeners
    addPanelBtn.addEventListener('click', function (e) {
        e.preventDefault();
        togglePanel(addPanel);
    });

    chatbotOpenBtn.addEventListener('click', function (e) {
        e.preventDefault();
        togglePanel(botPanel);
    });

    addPanelClose.addEventListener('click', function (e) {
        e.preventDefault();
        closeAllPanels();
    });

    // Close panels when clicking outside
    document.addEventListener('click', function (e) {
        const wrapper = document.querySelector('.floating-buttons-wrapper');
        if (!wrapper.contains(e.target)) {
            closeAllPanels();
        }
    });

    // Prevent panel clicks from closing the panel
    document.querySelectorAll('.panel').forEach(panel => {
        panel.addEventListener('click', function (e) {
            e.stopPropagation();
        });
    });

    const form = document.getElementById('chatbot-form');
    const container = document.getElementById('chatbot-container');

    if (form && container) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();

            const formData = new FormData(form);

            fetch('/Chat/ChatbotPartial', {
                method: 'POST',
                body: formData
            })
                .then(res => res.text())
                .then(html => {
                    container.innerHTML = html;
                    initChatbot();
                })
                .catch(err => console.error('Chatbot error:', err));
        });
    }
}
