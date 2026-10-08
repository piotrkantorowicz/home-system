import {
  Badge,
  Banner,
  Button,
  Card,
  DailyBars,
  Field,
  Input,
  LimitMeter,
  MacroBar,
  MetricTile,
  MoneyText,
  MonthStepper,
  Num,
  PageContainer,
  PageHeader,
  Ring,
  SegmentedControl,
  Select,
  StatusPill,
  Switch,
  Textarea,
} from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { goalStatus } from '@shared/lib/format';
import { formatNumber, formatSigned } from '@shared/lib/utils';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { ReactNode } from 'react';

function Group({ title, sub, children }: { title: string; sub: string; children: ReactNode }) {
  return (
    <Card className="p-22px flex flex-col gap-4">
      <div>
        <div className="text-body font-bold">{title}</div>
        <div className="text-muted-foreground text-label">{sub}</div>
      </div>
      {children}
    </Card>
  );
}

export default function ControlKit() {
  const { t } = useTranslation();
  const f = useFormat();
  const [segment, setSegment] = useState('table');
  const [on, setOn] = useState(true);

  return (
    <PageContainer className="animate-fade-in flex flex-col gap-5">
      <PageHeader
        title={t('control_kit.title')}
        subtitle={t('control_kit.subtitle')}
        className="mb-0"
      />

      <div className="gap-18px grid [grid-template-columns:repeat(auto-fit,minmax(330px,1fr))] items-start">
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
            <span className="text-meta">{on ? 'On' : 'Off'}</span>
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

        <Group
          title={t('control_kit.data')}
          sub="MetricTile · MacroBar · MoneyText · LimitMeter · MonthStepper"
        >
          <div className="grid grid-cols-2 gap-2">
            <MetricTile label="Calories" value={formatNumber(2150)} hint="kcal" />
            <MetricTile label="Delta" value={formatSigned(-130)} hint="vs target" accent="fat" />
          </div>
          <MacroBar label="Protein" value={95} target={140} macro="protein" unit="g" />
          <MacroBar label="Carbs" value={210} target={190} macro="carbs" unit="g" />
          <MacroBar
            label="Fibre (minimum goal — no overflow mark)"
            value={34}
            target={30}
            macro="fiber"
            unit="g"
            showOverflow={false}
          />
          <MoneyText amount="4114.65" currency="PLN" className="text-section font-bold" />
          <LimitMeter percent={62} />
          <LimitMeter percent={100} over limitAt={50} />
          <MonthStepper
            label="October 2026"
            groupLabel="Month"
            previousLabel="Previous month"
            nextLabel="Next month"
            onPrevious={() => undefined}
            onNext={() => undefined}
          />
        </Group>

        <Group title="DailyBars" sub="value labels · target line · today · missing vs zero">
          <DailyBars
            ariaLabel="Last 7 days"
            overLabel="over target"
            missingText="—"
            target={2100}
            targetLabel="2 100 target"
            days={[
              {
                key: 'a',
                label: 'Mon',
                sublabel: '28.9',
                value: 1900,
                text: '1 900',
                srText: 'Mon: 1 900 kcal',
              },
              {
                key: 'b',
                label: 'Tue',
                sublabel: '29.9',
                value: 2400,
                text: '2 400',
                over: true,
                srText: 'Tue: 2 400 kcal',
              },
              {
                key: 'c',
                label: 'Wed',
                sublabel: '30.9',
                value: null,
                text: '',
                srText: 'Wed: nothing logged',
              },
              {
                key: 'd',
                label: 'Thu',
                sublabel: '1.10',
                value: 0,
                text: '0',
                srText: 'Thu: 0 kcal',
              },
              {
                key: 'e',
                label: 'Fri',
                sublabel: '2.10',
                value: 1500,
                text: '1 500',
                isToday: true,
                srText: 'Fri: 1 500 kcal',
              },
            ]}
          />
        </Group>

        <Group title="DailyBars · compact" sub="30/90 days: no figures, weekly labels">
          <DailyBars
            compact
            ariaLabel="Last 30 days"
            overLabel="over target"
            missingText="—"
            target={2100}
            days={Array.from({ length: 30 }, (_, i) => ({
              key: String(i),
              label: i % 7 === 0 ? `${String(i + 1)}.9` : '',
              sublabel: '',
              value: i % 6 === 5 ? null : 1700 + ((i * 137) % 700),
              text: '',
              srText: `Day ${String(i + 1)}`,
            }))}
          />
        </Group>

        <Group title="DailyBars · water" sub="tone=water · goal line · missing vs logged">
          <DailyBars
            tone="water"
            ariaLabel="Last 3 days of water"
            overLabel=""
            missingText="—"
            target={2500}
            targetLabel="2.5 L goal"
            days={[
              {
                key: 'w1',
                label: 'Mon',
                sublabel: '28.9',
                value: 2600,
                text: '2.6',
                srText: 'Mon: 2.6 L',
              },
              {
                key: 'w2',
                label: 'Tue',
                sublabel: '29.9',
                value: null,
                text: '',
                srText: 'Tue: nothing logged',
              },
              {
                key: 'w3',
                label: 'Wed',
                sublabel: '30.9',
                value: 1300,
                text: '1.3',
                isToday: true,
                srText: 'Wed: 1.3 L',
              },
            ]}
          />
        </Group>

        <Group title={t('control_kit.ring')} sub="conic progress">
          <div className="flex items-center gap-4">
            <Ring percent={72} size={110} thickness={11}>
              <span className="numeral text-section font-bold">72%</span>
            </Ring>
            <Ring percent={112} size={110} thickness={11} color="fat">
              <span className="numeral text-section font-bold">112%</span>
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
        <Group
          title="Page header"
          sub="PageHeader · PageContainer — title = nav label, one primary action, 1120px body, centred"
        >
          <PageHeader
            title="Expenses"
            subtitle="Sat 3 Oct"
            breadcrumb={[{ label: 'Budget', href: '/budget' }, { label: 'Expenses' }]}
            actions={
              <>
                <Button size="xl" variant="secondary">
                  Export
                </Button>
                <Button size="xl">Add expense</Button>
              </>
            }
            className="mb-0"
          />
        </Group>
        <Group
          title="Formatting"
          sub="useFormat() · Num · goalStatus — follows language and unit preferences"
        >
          <div className="text-body flex flex-col gap-1">
            <Num>{f.energy(2100)}</Num>
            <Num>{f.grams(4.54)}</Num>
            <Num>{f.weight(72.46)}</Num>
            <Num>{f.volume(250)}</Num>
            <Num>{f.waterProgress(1330, 2500)}</Num>
            <Num>{f.money('4114.65')}</Num>
            <Num>{f.quantity(0.5, 'Piece')}</Num>
            <Num>{f.dayShort('2026-10-03')}</Num>
          </div>
          <div className="text-meta text-muted-foreground">
            2200 / 2000 kcal: {goalStatus(2200, 2000, 'limit').state} · 100 / 150 g protein:{' '}
            {goalStatus(100, 150, 'min').state}
          </div>
        </Group>
      </div>
    </PageContainer>
  );
}
