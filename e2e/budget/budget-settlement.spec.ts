import { test, expect } from '../diet-planner/fixtures';

// Each worker owns its own household, so budget state never crosses workers.
test.describe('Budget settle up', () => {
  test('an adult sees the all-recorded-entries balance page with its explanation, on a phone too', async ({
    page,
  }) => {
    await page.goto('/budget');
    const setup = page.getByRole('button', { name: 'Set up Budget' });
    const overview = page.getByRole('heading', { name: 'Envelopes' });
    await expect(setup.or(overview)).toBeVisible();
    if (await setup.isVisible()) await setup.click();
    await expect(overview).toBeVisible();

    // The adult-only entry is in the navigation.
    await page.getByRole('link', { name: 'Settle up' }).first().click();
    await expect(
      page.getByRole('heading', { name: 'Outstanding balance — all recorded entries' }),
    ).toBeVisible();
    await expect(page.getByRole('heading', { name: 'How this is worked out' })).toBeVisible();
    await expect(page.getByText(/Future-dated entries count immediately/)).toBeVisible();

    // A one-adult household has nobody to owe: a real settled state, not an error or a blank.
    await expect(page.getByText('Everyone is settled up')).toBeVisible();

    // Recording stays available when settled, with an empty history. Two-adult payment journeys: #456.
    await expect(page.getByRole('button', { name: 'Record payment' })).toBeVisible();
    await expect(page.getByText('No payments recorded yet.')).toBeVisible();

    await page.setViewportSize({ width: 390, height: 844 });
    await expect(
      page.getByRole('heading', { name: 'Outstanding balance — all recorded entries' }),
    ).toBeVisible();
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    );
    expect(overflow).toBeLessThanOrEqual(0);
  });
});
