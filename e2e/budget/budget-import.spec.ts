import { test, expect } from '../diet-planner/fixtures';

import { archiveTrackedEnvelopes, createEnvelope, ensureBudget, today } from './utils/seed';

/**
 * A synthetic mBank export (UTF-8 is accepted alongside Windows-1250). The account number is new
 * every run, so the fingerprint source keys never match an earlier run's import; the amounts are
 * new too, so no earlier expense shows up as a possible duplicate.
 */
function mbankStatement(run: string, rows: { merchant: string; amount: string }[]): Buffer {
  const day = today();
  const period = day.split('-').reverse().join('.');
  const account = Array.from({ length: 26 }, () => Math.floor(Math.random() * 10)).join('');
  const lines = [
    'mBank S.A. Bankowość Detaliczna;',
    '#Za okres:;',
    `${period};${period};`,
    '#Waluta;',
    'PLN;',
    '#Numer rachunku;',
    `${account};`,
    ';',
    '#Data księgowania;#Data operacji;#Opis operacji;#Tytuł;#Nadawca/Odbiorca;#Numer konta;#Kwota;#Saldo po operacji;',
    ...rows.map(
      (r) =>
        `${day};${day};ZAKUP PRZY UŻYCIU KARTY;"${r.merchant} ${run} /WARSZAWA DATA TRANSAKCJI: ${day}";;'';-${r.amount.replace('.', ',')};0,00;`,
    ),
    ';',
  ];
  return Buffer.from(lines.join('\r\n'), 'utf8');
}

// Each worker owns its own household, so the envelope and its imports never cross workers.
test.describe('Budget statement import', () => {
  test.afterEach(async ({ page }) => archiveTrackedEnvelopes(page));

  test('owner imports a statement into an envelope, and the same file again imports nothing', async ({
    page,
  }) => {
    const run = Date.now().toString(36).toUpperCase();
    const cents = String(10 + (Date.now() % 89));
    const rows = [
      { merchant: 'E2E APTEKA', amount: `${String(100 + (Date.now() % 400))}.${cents}` },
      { merchant: 'E2E SKLEP', amount: `${String(500 + (Date.now() % 400))}.${cents}` },
    ];
    const file = { name: 'mbank.csv', mimeType: 'text/csv', buffer: mbankStatement(run, rows) };
    const envelope = `E2E import ${run}`;

    await page.goto('/budget');
    await ensureBudget(page);
    await createEnvelope(page, envelope, 'Household');

    await page.goto('/budget/expenses');
    await page.getByRole('link', { name: 'Import statement' }).click();
    const form = page.getByRole('form', { name: 'Import bank statement' });
    await form.getByLabel('Envelope').selectOption({ label: envelope });
    await form.getByLabel('Statement file').setInputFiles(file);
    // Household funds keep the journey independent of other members.
    await form.getByLabel('Paid from').selectOption('HouseholdFunds');
    await form.getByRole('button', { name: 'Preview' }).click();

    const review = page.getByRole('region', { name: 'Statement rows' });
    await expect(review.getByText('New', { exact: true })).toHaveCount(2);
    await review
      .getByRole('combobox', { name: new RegExp(`Category for E2E APTEKA ${run}`) })
      .selectOption('Health');
    await review.getByRole('button', { name: 'Import 2 expenses' }).click();

    await expect(page.getByText('2 expenses imported')).toBeVisible();
    await page.getByRole('link', { name: 'View expenses' }).click();
    await expect(page).toHaveURL('/budget/expenses');
    for (const { amount } of rows) {
      await expect(
        page.getByRole('link', { name: new RegExp(amount.replace('.', '\\.')) }).first(),
      ).toBeVisible();
    }

    // The same statement again: every row is already imported, so nothing can be confirmed.
    await page.getByRole('link', { name: 'Import statement' }).click();
    await form.getByLabel('Envelope').selectOption({ label: envelope });
    await form.getByLabel('Statement file').setInputFiles(file);
    await form.getByRole('button', { name: 'Preview' }).click();
    await expect(review.getByText('Already imported', { exact: true })).toHaveCount(2);
    for (const checkbox of await review.getByRole('checkbox').all()) {
      await expect(checkbox).toBeDisabled();
    }
    await expect(review.getByRole('button', { name: 'Import 0 expenses' })).toBeDisabled();
  });
});
