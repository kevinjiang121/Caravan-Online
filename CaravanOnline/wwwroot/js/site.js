let selectedCardFace = null;
let selectedCardFull = null;
let discardLaneMode = false;
let discardCardMode = false;

function setUserMessage(msg) {
    document.getElementById('userMessage').innerText = msg;
}

function activateDiscardLane() {
    discardLaneMode = true;
    discardCardMode = false;
    setUserMessage("Click on the lane button you want to discard.");
}

function activateDiscardCard() {
    discardLaneMode = false;
    discardCardMode = true;
    setUserMessage("Click on a card in your HAND to discard it.");
}

function highlightCards(face, fullCard) {
    if (discardCardMode) {
        discardSelectedCard(face, fullCard);
        return;
    }
    selectedCardFace = face;
    selectedCardFull = fullCard;
    document.getElementById('selectedCardInput').value = fullCard;

    const laneCards = document.querySelectorAll('.lane-card');
    laneCards.forEach(laneCard => {
        if (face === 'K' || face === 'Q' || face === 'J') {
            laneCard.classList.add('highlight');
            laneCard.style.border = '2px solid red'; // Visual highlight
        } else {
            laneCard.classList.remove('highlight');
            laneCard.style.border = 'none';
        }
    });

    if (face !== 'K' && face !== 'Q' && face !== 'J') {
        document.getElementById('card-selection-form').submit();
    }
}

function discardSelectedCard(face, fullCard) {
    const suit = fullCard.split(' ')[1];
    fetch('/?handler=DiscardCardClick', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getRequestVerificationToken()
        },
        body: JSON.stringify({ Face: face, Suit: suit })
    })
    .then(r => r.json())
    .then(result => {
        if (result.success) {
            location.reload();
        } else {
            setUserMessage(result.message || "Error discarding card.");
        }
    })
    .catch(err => {
        setUserMessage("Discard card error.");
    })
    .finally(() => {
        discardCardMode = false;
    });
}

document.querySelectorAll('[name="selectedLane"]').forEach(btn => {
    btn.addEventListener('click', evt => {
        if (discardLaneMode) {
            evt.preventDefault();
            const lane = evt.currentTarget.value;
            fetch('/?handler=DiscardLaneClick', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getRequestVerificationToken()
                },
                body: JSON.stringify({ Lane: lane })
            })
            .then(r => r.json())
            .then(result => {
                if (result.success) {
                    location.reload();
                } else {
                    setUserMessage(result.message || "Error discarding lane.");
                }
            })
            .catch(err => {
                setUserMessage("Discard lane error.");
            })
            .finally(() => {
                discardLaneMode = false;
            });
        }
    });
});

function placeCardNextTo(event, cardIndex) {
    if (selectedCardFace !== 'K' && selectedCardFace !== 'Q' && selectedCardFace !== 'J') return;
    const cardElement = event.target.closest('.lane-card');
    const cardFace = cardElement.getAttribute('data-card').split(' ')[0];
    const cardSuit = cardElement.getAttribute('data-card').split(' ')[1];
    const lane = parseInt(cardElement.getAttribute('data-lane'));
    const index = cardIndex;

    fetch('/?handler=PlaceCardNextTo', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getRequestVerificationToken()
        },
        body: JSON.stringify({
            Card: `${cardFace} ${cardSuit}`,
            AttachedCard: selectedCardFull,
            CardIndex: index,
            Lane: lane
        })
    })
    .then(response => response.json())
    .then(result => {
        if (result.success) {
            location.reload();
        } else {
            alert(result.message || "Error attaching card.");
        }
    })
    .catch(err => {
        alert("Attach card error.");
    });

    resetHighlights();
}

function resetHighlights() {
    document.querySelectorAll('.lane-card').forEach(laneCard => {
        laneCard.classList.remove('highlight');
        laneCard.style.border = 'none'; // Remove visual highlight
    });
    selectedCardFace = null;
    selectedCardFull = null;
}

function cardClicked(face, suit, index) {
    if (selectedCardFace === 'K' || selectedCardFace === 'Q' || selectedCardFace === 'J') {
        event.preventDefault();
        placeCardNextTo(event, index);
    }
}

function getRequestVerificationToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : '';
}
