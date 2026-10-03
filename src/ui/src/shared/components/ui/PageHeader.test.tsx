import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';

import { PageContainer, PageHeader } from './PageHeader';

describe('PageHeader', () => {
  it('renders the title as the h1 with subtitle and actions', () => {
    render(<PageHeader title="Expenses" subtitle="Sat 3 Oct" actions={<button>Add</button>} />);
    expect(screen.getByRole('heading', { level: 1, name: 'Expenses' })).toBeInTheDocument();
    expect(screen.getByText('Sat 3 Oct')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Add' })).toBeInTheDocument();
  });

  it('renders a breadcrumb whose last crumb is the current page, not a link', () => {
    render(
      <MemoryRouter>
        <PageHeader
          title="Lidl"
          breadcrumb={[
            { label: 'Budget', href: '/budget' },
            { label: 'Expenses', href: '/budget/expenses' },
            { label: 'Lidl' },
          ]}
        />
      </MemoryRouter>,
    );
    expect(screen.getByRole('link', { name: 'Budget' })).toHaveAttribute('href', '/budget');
    expect(screen.getByRole('link', { name: 'Expenses' })).toBeInTheDocument();
    expect(screen.getByText('Lidl', { selector: 'span' })).toHaveAttribute('aria-current', 'page');
  });

  it('omits empty optional parts', () => {
    render(<PageHeader title="Today" />);
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument();
  });
});

describe('PageContainer', () => {
  it('applies the narrower width inside the standard container', () => {
    render(<PageContainer width="form">body</PageContainer>);
    expect(screen.getByText('body')).toHaveClass('max-w-[880px]');
  });
});
