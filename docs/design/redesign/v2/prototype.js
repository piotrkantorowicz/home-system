// Standalone design prototype. All data is illustrative and kept in memory.
const meals = [
  { name: 'Overnight oats with blueberries', kind: 'Breakfast', time: '08:00', kcal: 520, protein: 32, carbs: 62, fat: 16, fiber: 8, eaten: true },
  { name: 'Chicken, rice & roasted vegetables', kind: 'Lunch', time: '12:30', kcal: 720, protein: 64, carbs: 80, fat: 26, fiber: 10, eaten: true },
  { name: 'Skyr with raspberries', kind: 'Snack', time: '16:30', kcal: 210, protein: 22, carbs: 24, fat: 3, fiber: 4, eaten: false },
  { name: 'Salmon & crispy potatoes', kind: 'Dinner', time: '19:30', kcal: 560, protein: 34, carbs: 48, fat: 24, fiber: 5, eaten: false },
  { name: 'Apple & a few almonds', kind: 'Evening snack', time: '21:00', kcal: 180, protein: 5, carbs: 22, fat: 9, fiber: 4, eaten: false },
];
let nextId = 5;
let water = [{ id: 1, amount: 500 }, { id: 2, amount: 250 }, { id: 3, amount: 500 }, { id: 4, amount: 150 }];
let undoAction = null;
let selectedDay = 3;
const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
const dates = [31, 1, 2, 3, 4, 5, 6];
const $ = (selector) => document.querySelector(selector);
const number = (value) => value.toLocaleString('en-GB');

function announce(message, undo) {
  undoAction = undo;
  $('#announcement').textContent = message;
  $('#toast').hidden = false;
}

function render() {
  const logged = meals.filter((meal) => meal.eaten);
  const eaten = logged.reduce((sum, meal) => sum + meal.kcal, 0);
  $('#remaining').textContent = number(Math.abs(2150 - eaten));
  $('#remaining-label').textContent = eaten > 2150 ? 'kcal above target' : 'kcal remaining';
  $('#eaten').textContent = number(eaten);
  $('#meal-count').textContent = `${logged.length} of ${meals.length} meals logged`;
  $('#energy-progress').value = eaten;
  $('#macros').innerHTML = [['Protein', 'protein', 140, 'protein'], ['Carbs', 'carbs', 240, 'carbs'], ['Fat', 'fat', 70, 'fat'], ['Fibre', 'fiber', 30, 'water']].map(([label, key, target, color]) => {
    const actual = logged.reduce((sum, meal) => sum + meal[key], 0);
    return `<div class="macro" style="--macro:var(--${color})"><div><b>${label}</b><span>${actual} / ${target} g</span></div><progress max="${target}" value="${actual}" aria-label="${label}: ${actual} of ${target} grams"></progress></div>`;
  }).join('');
  const next = meals.findIndex((meal) => !meal.eaten);
  const mealRows = meals.map((meal, index) => `<article class="meal ${index === next ? 'meal-next' : ''}"><time>${meal.time}</time><div><div class="meal-kind">${meal.kind}${index === next ? ' · Next on your plan' : ''}</div><h3>${meal.name}</h3><p>${meal.kcal} kcal <span class="divider">/</span> ${meal.protein} g protein</p>${meal.eaten ? '<span class="meal-status">✓ Logged</span>' : `<div class="meal-actions"><button data-meal="${index}" class="${index === next ? 'primary' : ''}">Mark eaten</button></div>`}</div></article>`);
  $('#meal-list').innerHTML = mealRows.filter((_, index) => !meals[index].eaten).join('') + `<details class="logged-meals"><summary>Logged meals (${logged.length})</summary>${mealRows.filter((_, index) => meals[index].eaten).join('')}</details>`;
  const total = water.reduce((sum, entry) => sum + entry.amount, 0);
  $('#water-total').textContent = number(total);
  $('#water-progress').value = total;
  $('#water-remaining').textContent = total < 2500 ? `${number(2500 - total)} ml to your daily target` : 'Daily target reached';
  $('#water-count').textContent = water.length;
  $('#water-log').innerHTML = water.map((entry) => `<li><span>${entry.amount} ml</span><button data-remove="${entry.id}" aria-label="Remove water entry ${entry.id}, ${entry.amount} ml">Remove</button></li>`).join('');
  $('#today-bar').style.setProperty('--bar', `${Math.min(100, eaten / 2500 * 100)}%`);
  $('.week-bars').setAttribute('aria-label', `Calories logged: Monday 2,030; Tuesday 2,180; Wednesday 2,010; Thursday ${number(eaten)} so far. Friday to Sunday have no entries.`);
  renderPlan();
}

function renderPlan() {
  $('#day-picker').innerHTML = days.map((day, index) => `<button data-day="${index}" aria-pressed="${index === selectedDay}" aria-label="${day}, ${dates[index]} ${index === 0 ? 'August' : 'September'}"><span>${day.slice(0, 3)}</span><b>${dates[index]}</b></button>`).join('');
  $('#selected-day').textContent = `${days[selectedDay]}, ${dates[selectedDay]} ${selectedDay === 0 ? 'August' : 'September'}`;
  const planned = selectedDay === 3 ? meals : [];
  $('#planned-total').textContent = planned.length ? `${number(planned.reduce((sum, meal) => sum + meal.kcal, 0))} kcal planned` : 'No sample meals';
  $('#plan-meals').innerHTML = planned.length ? planned.map((meal) => `<article class="meal"><time>${meal.time}</time><div><div class="meal-kind">${meal.kind}</div><h3>${meal.name}</h3><p>${meal.kcal} kcal · ${meal.eaten ? 'Logged' : 'Planned'}</p></div></article>`).join('') : '<p>This demo includes Thursday’s meals only. Select Thursday to explore the plan.</p>';
}

function navigate() {
  const plan = location.hash === '#plan';
  $('#today-view').hidden = plan;
  $('#plan-view').hidden = !plan;
  $('#breadcrumb').textContent = plan ? 'Meal plan' : 'Today';
  $('#page-title').textContent = plan ? 'A plan that fits your week.' : 'Make room for your day.';
  $('#page-description').textContent = plan ? 'See what’s planned before the day begins.' : 'Your meals, your progress, one place.';
  document.querySelectorAll('[data-page]').forEach((link) => {
    if (link.dataset.page === (plan ? 'plan' : 'today')) link.setAttribute('aria-current', 'page');
    else link.removeAttribute('aria-current');
  });
}

function addWater(amount) {
  const entry = { id: nextId++, amount };
  water.push(entry);
  render();
  announce(`${amount} ml added`, () => { water = water.filter((item) => item.id !== entry.id); });
}

document.addEventListener('click', (event) => {
  const button = event.target.closest('button');
  if (!button) return;
  if (button.dataset.water) addWater(Number(button.dataset.water));
  if (button.dataset.meal) {
    const index = Number(button.dataset.meal);
    meals[index].eaten = true;
    render();
    announce(`${meals[index].kind} logged`, () => { meals[index].eaten = false; });
    $('#undo').focus();
  }
  if (button.dataset.remove) {
    const id = Number(button.dataset.remove);
    const index = water.findIndex((entry) => entry.id === id);
    const [entry] = water.splice(index, 1);
    render();
    announce(`${entry.amount} ml removed`, () => { water.splice(index, 0, entry); });
    $('#undo').focus();
  }
  if (button.dataset.day) {
    selectedDay = Number(button.dataset.day);
    renderPlan();
    document.querySelector(`[data-day="${selectedDay}"]`).focus();
  }
});
$('#undo').addEventListener('click', () => {
  if (!undoAction) return;
  undoAction();
  undoAction = null;
  render();
  $('#announcement').textContent = 'Last action undone';
});
$('#dismiss').addEventListener('click', () => { $('#toast').hidden = true; undoAction = null; $('#theme').focus(); });
$('#theme').addEventListener('click', () => {
  const dark = document.documentElement.dataset.theme !== 'dark';
  document.documentElement.dataset.theme = dark ? 'dark' : 'light';
  $('#theme').textContent = dark ? 'Light mode' : 'Dark mode';
  $('#theme').setAttribute('aria-pressed', String(dark));
});
$('#custom-water').addEventListener('click', () => $('#water-dialog').showModal());
$('#cancel').addEventListener('click', () => $('#water-dialog').close());
$('#water-form').addEventListener('submit', (event) => {
  event.preventDefault();
  if (!$('#water-form').reportValidity()) return;
  addWater(Number($('#amount').value));
  $('#water-dialog').close();
});
window.addEventListener('hashchange', navigate);
render();
navigate();
