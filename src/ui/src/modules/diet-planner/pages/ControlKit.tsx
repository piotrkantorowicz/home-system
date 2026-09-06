import {
  Badge,
  Banner,
  Button,
  Card,
  Field,
  Input,
  MacroBar,
  MetricTile,
  Ring,
  SegmentedControl,
  Select,
  StatusPill,
  Switch,
  Textarea,
} from '@shared/components/ui';
import { formatNumber, formatSigned } from '@shared/lib/utils';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { ReactNode } from 'react';

function Group({ title, sub, children }: { title: string; sub: string; children: ReactNode }) {
  return (
    <Card className="flex flex-col gap-4 p-[22px]">
      <div>
        <div className="text-[15px] font-bold">{title}</div>
        <div className="text-muted-foreground text-[12.5px]">{sub}</div>
      </div>
      {children}
    </Card>
  );
}

export default function ControlKit() {
  const { t } = useTranslation();
  const [segment, setSegment] = useState('table');
  const [on, setOn] = useState(true);

  return (
    <div className="animate-fade-in mx-auto flex max-w-6xl flex-col gap-5 px-4 py-6 md:px-8">
      <div>
        <h1 className="text-[26px] font-bold tracking-tight">{t('control_kit.title')}</h1>
        <p className="text-muted-foreground mt-1 text-sm">{t('control_kit.subtitle')}</p>
      </div>

      <div className="grid [grid-template-columns:repeat(auto-fit,minmax(330px,1fr))] items-start gap-[18px]">
        <Group
          title={t('control_kit.buttons')}
          sub="42 / 34 / 30px — primary · secondary · ghost · destructive"
        >
          <div className="flex flex-wrap items-center gap-2">
            <Button size="xl">Primary</Button>
            <Button size="xl" variant="secondary">
              Secondary
            </Button>
            <Button size="xl" variant="ghost">
              Ghost
            </Button>
            <Button size="xl" variant="destructive">
              Destructive
            </Button>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <Button size="sm">Small</Button>
            <Button size="chip" variant="secondary">
              Chip
            </Button>
            <Button size="xs" variant="secondary">
              Row action
            </Button>
            <Button size="xl" disabled>
              Disabled
            </Button>
          </div>
        </Group>

        <Group title={t('control_kit.inputs')} sub="default · focus · invalid · disabled">
          <Field id="ck-name" label="Label">
            <Input id="ck-name" placeholder="Placeholder" />
          </Field>
          <Field id="ck-bad" label="Invalid" error="This field is required">
            <Input id="ck-bad" aria-invalid defaultValue="12" />
          </Field>
          <Field id="ck-sel" label="Select">
            <Select id="ck-sel" defaultValue="g">
              <option value="g">grams</option>
              <option value="ml">millilitres</option>
            </Select>
          </Field>
          <Field id="ck-ta" label="Textarea">
            <Textarea id="ck-ta" placeholder="Notes" />
          </Field>
        </Group>

        <Group title={t('control_kit.toggles')} sub="Switch · SegmentedControl">
          <div className="flex items-center gap-3">
            <Switch checked={on} onCheckedChange={setOn} aria-label="demo switch" />
            <span className="text-[13px]">{on ? 'On' : 'Off'}</span>
          </div>
          <SegmentedControl
            label="View"
            value={segment}
            onChange={setSegment}
            options={[
              { value: 'table', label: 'Table' },
              { value: 'cards', label: 'Cards' },
            ]}
          />
        </Group>

        <Group title={t('control_kit.pills')} sub="StatusPill · Badge">
          <div className="flex flex-wrap gap-2">
            <StatusPill variant="good">On track</StatusPill>
            <StatusPill variant="over">Over</StatusPill>
            <StatusPill variant="neutral">Neutral</StatusPill>
          </div>
          <div className="flex flex-wrap gap-2">
            <Badge>Default</Badge>
            <Badge variant="secondary">Secondary</Badge>
            <Badge variant="destructive">Incomplete</Badge>
            <Badge variant="outline">Outline</Badge>
          </div>
        </Group>

        <Group title={t('control_kit.data')} sub="MetricTile · MacroBar · numerals">
          <div className="grid grid-cols-2 gap-2">
            <MetricTile label="Calories" value={formatNumber(2150)} hint="kcal" />
            <MetricTile label="Delta" value={formatSigned(-130)} hint="vs target" accent="fat" />
          </div>
          <MacroBar label="Protein" value={95} target={140} macro="protein" unit="g" />
          <MacroBar label="Carbs" value={210} target={190} macro="carbs" unit="g" />
        </Group>

        <Group title={t('control_kit.ring')} sub="conic progress">
          <div className="flex items-center gap-4">
            <Ring percent={72} size={110} thickness={11}>
              <span className="numeral text-[20px] font-bold">72%</span>
            </Ring>
            <Ring percent={112} size={110} thickness={11} color="fat">
              <span className="numeral text-[20px] font-bold">112%</span>
            </Ring>
          </div>
        </Group>

        <Group title={t('control_kit.banners')} sub="success · warning · error · info">
          <Banner variant="success">Saved.</Banner>
          <Banner variant="warning">Some units are missing.</Banner>
          <Banner variant="error" onRetry={() => undefined}>
            Could not load.
          </Banner>
          <Banner variant="info">Formats apply on this device only.</Banner>
        </Group>
      </div>
    </div>
  );
}
