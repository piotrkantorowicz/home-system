import { describe, expect, it } from 'vitest';

import { shortEventType } from './format';

describe('shortEventType', () => {
  it('keeps only the type name of an assembly-qualified name', () => {
    expect(
      shortEventType(
        'Household.Contracts.Events.MemberJoinedIntegrationEvent, Household.Contracts',
      ),
    ).toBe('MemberJoinedIntegrationEvent');
  });

  it('returns an unqualified name unchanged', () => {
    expect(shortEventType('Plain')).toBe('Plain');
  });
});
