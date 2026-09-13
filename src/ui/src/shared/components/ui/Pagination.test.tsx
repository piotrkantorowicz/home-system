import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';

import { Pagination } from './Pagination';

describe('Pagination', () => {
  it('reports a page-size change once and leaves the page reset to the consumer', async () => {
    const onPageChange = vi.fn();
    const onPageSizeChange = vi.fn();
    render(
      <Pagination
        page={3}
        pageSize={25}
        totalCount={120}
        onPageChange={onPageChange}
        onPageSizeChange={onPageSizeChange}
      />,
    );

    await userEvent.selectOptions(screen.getByRole('combobox'), '10');

    expect(onPageSizeChange).toHaveBeenCalledExactlyOnceWith(10);
    expect(onPageChange).not.toHaveBeenCalled();
  });

  it('moves to the next page when the next button is clicked', async () => {
    const onPageChange = vi.fn();
    render(
      <Pagination
        page={1}
        pageSize={25}
        totalCount={60}
        onPageChange={onPageChange}
        onPageSizeChange={vi.fn()}
      />,
    );

    await userEvent.click(screen.getByRole('button', { name: /next/i }));

    expect(onPageChange).toHaveBeenCalledExactlyOnceWith(2);
  });
});
