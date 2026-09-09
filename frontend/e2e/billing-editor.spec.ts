import { expect, test } from '@playwright/test';
import { mockAdminApis, openBillingEditor } from './helpers/billing-api-mock';

test.describe('Memorial editor billing tab', () => {
  test('renewal updates billing state without leaving tab or reloading', async ({ page }) => {
    await mockAdminApis(page, { initialStatus: 'Published', afterRenewStatus: 'Published' });
    await openBillingEditor(page);

    const billingTab = page.getByRole('button', { name: 'Оплата та продовження' });
    await expect(billingTab).toHaveClass(/border-memorial-ink/);

    await page.getByRole('button', { name: 'Підтвердити продовження на 1 рік' }).click();

    const dialog = page.getByRole('heading', { name: 'Підтвердити продовження на 1 рік' });
    await expect(dialog).toBeVisible();
    await expect(page.getByText('Початок періоду')).toBeVisible();
    await expect(page.getByText('08.09.2099').first()).toBeVisible();
    await expect(page.getByText('08.09.2100').first()).toBeVisible();
    await expect(page.getByText('08.10.2100').first()).toBeVisible();

    const amount = page.locator('input[name="confirmAmount"]');
    await expect(amount).toHaveValue('300');

    await page.getByRole('button', { name: 'Підтвердити', exact: true }).click();

    await expect(page.getByText('Продовження на 1 рік підтверджено')).toBeVisible();
    await expect(billingTab).toHaveClass(/border-memorial-ink/);
    await expect(page.getByText('08.09.2100').first()).toBeVisible();
    await expect(page.getByText('08.10.2100').first()).toBeVisible();
    await expect(page.getByText('Оплачено').first()).toBeVisible();
    await expect(page.getByRole('cell', { name: 'Продовження' })).toBeVisible();
  });

  test('suspended memorial becomes Published in header after renewal', async ({ page }) => {
    await mockAdminApis(page, { initialStatus: 'Suspended', afterRenewStatus: 'Published' });
    await openBillingEditor(page);

    await expect(page.getByText(/Статус:\s*Призупинено/)).toBeVisible();

    await page.getByRole('button', { name: 'Підтвердити продовження на 1 рік' }).click();
    await page.getByRole('button', { name: 'Підтвердити', exact: true }).click();

    await expect(page.getByText('Продовження на 1 рік підтверджено')).toBeVisible();
    await expect(page.getByText(/Статус:\s*Опубліковано/)).toBeVisible();
    await expect(page.getByRole('button', { name: 'Оплата та продовження' })).toHaveClass(/border-memorial-ink/);
  });

  test('draft memorial stays Draft in header after renewal', async ({ page }) => {
    await mockAdminApis(page, { initialStatus: 'Draft', afterRenewStatus: 'Draft' });
    await openBillingEditor(page);

    await expect(page.getByText(/Статус:\s*Чернетка/)).toBeVisible();

    await page.getByRole('button', { name: 'Підтвердити продовження на 1 рік' }).click();
    await page.getByRole('button', { name: 'Підтвердити', exact: true }).click();

    await expect(page.getByText('Продовження на 1 рік підтверджено')).toBeVisible();
    await expect(page.getByText(/Статус:\s*Чернетка/)).toBeVisible();
    await expect(page.getByRole('button', { name: 'Оплата та продовження' })).toHaveClass(/border-memorial-ink/);
  });
});
