import { test, expect } from '@playwright/test';

test('registration, persistent task CRUD, filtering, confirmation and logout', async ({
  page,
}, testInfo) => {
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  page.on('console', (message) => {
    if (message.type() === 'error') errors.push(message.text());
  });
  const email = `browser-${Date.now()}-${testInfo.project.name}@example.test`;
  const password = 'Browser-demo-password1!';
  await page.goto('/tasks');
  await expect(page).toHaveURL(/\/login$/);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page.getByText('Enter a valid email address.')).toBeVisible();
  await page.getByRole('link', { name: 'Create an account' }).click();
  await expect(page.getByRole('heading', { name: 'Create your account' })).toBeVisible();
  await page.getByLabel('Email address').fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Create account', exact: true }).click();
  await expect(page.getByRole('status')).toContainText('Account created');
  await page.getByLabel('Email address').fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Start with one small step.' })).toBeVisible();
  await page.getByRole('link', { name: 'New task', exact: false }).click();
  await page.getByLabel('Title', { exact: false }).fill('Prepare <b>interview</b>');
  await page
    .getByLabel('Description', { exact: false })
    .fill('Explain SQL ownership and JWT validation.');
  await page.getByLabel('Due date', { exact: false }).fill('2025-01-01T12:30');
  await page.getByRole('button', { name: 'Create task', exact: true }).click();
  await expect(
    page.getByRole('heading', { name: 'Prepare <b>interview</b>', exact: true }),
  ).toBeVisible();
  await expect(page.getByText('Overdue', { exact: false })).toBeVisible();
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Prepare <b>interview</b>' })).toBeVisible();
  await page.getByLabel('Status for Prepare <b>interview</b>').selectOption('InProgress');
  await expect(page.locator('.badge')).toHaveText('In progress');
  await page.getByRole('button', { name: 'Completed', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Nothing in this view.' })).toBeVisible();
  await page.getByRole('button', { name: 'All', exact: true }).click();
  await page.getByRole('link', { name: 'Edit Prepare <b>interview</b>' }).click();
  await page.getByLabel('Title', { exact: false }).fill('Interview ready');
  await page.getByLabel('Status', { exact: true }).selectOption('Completed');
  await page.getByRole('button', { name: 'Save changes' }).click();
  await expect(page.getByRole('heading', { name: 'Interview ready' })).toBeVisible();
  await expect(page.locator('.badge')).toHaveText('Completed');
  await expect(page.getByText('Overdue', { exact: false })).toHaveCount(0);
  await page.screenshot({ path: testInfo.outputPath('dashboard.png'), fullPage: true });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(
    true,
  );
  await page.getByRole('button', { name: 'Delete Interview ready' }).click();
  await page.getByRole('button', { name: 'Keep task' }).click();
  await expect(page.getByRole('heading', { name: 'Interview ready' })).toBeVisible();
  await page.getByRole('button', { name: 'Delete Interview ready' }).click();
  await page.getByRole('button', { name: 'Delete task', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Start with one small step.' })).toBeVisible();
  await page.getByRole('button', { name: 'Sign out' }).click();
  await page.goto('/tasks');
  await expect(page).toHaveURL(/\/login$/);
  expect(errors).toEqual([]);
});

test('development demo account has sample tasks and responsive layout', async ({
  page,
}, testInfo) => {
  await page.goto('/login');
  await page.screenshot({ path: testInfo.outputPath('login.png'), fullPage: true });
  await page.getByLabel('Email address').fill('demo@taskmanager.local');
  await page.getByLabel('Password', { exact: true }).fill('Demo-TaskManager-2026!');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Prepare the interview demo' })).toBeVisible();
  await expect(page.locator('.task-card')).toHaveCount(3);
  await expect(
    page
      .locator('.task-card')
      .filter({ has: page.getByRole('heading', { name: 'Review architecture decisions' }) })
      .getByRole('combobox'),
  ).toHaveValue('InProgress');
  await expect(
    page
      .locator('.task-card')
      .filter({ has: page.getByRole('heading', { name: 'Run the automated checks' }) })
      .getByRole('combobox'),
  ).toHaveValue('Completed');
  await page.screenshot({ path: testInfo.outputPath('seeded-dashboard.png'), fullPage: true });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(
    true,
  );
});
