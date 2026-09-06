import { Banner, Button, Card, SegmentedControl, Switch } from '@shared/components/ui';
import { useTheme } from '@shared/context/ThemeContext';
import { usePreferences, type Preferences as Prefs } from '@shared/hooks/usePreferences';
import { cn, getInitials } from '@shared/lib/utils';
import { LogOut } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';

type ThemeChoice = 'light' | 'dark' | 'system';

export default function Preferences() {
  const { t, i18n } = useTranslation();
  const { theme, setTheme } = useTheme();
  const { prefs, set } = usePreferences();
  const auth = useAuth();

  const profile = auth.user?.profile;
  const displayName = profile?.name ?? profile?.preferred_username ?? profile?.email ?? 'User';
  const provider = profile?.iss ?? '';

  return (
    <div className="animate-fade-in mx-auto flex max-w-5xl flex-col gap-6 px-4 py-6 md:px-8">
      <div>
        <h1 className="text-[26px] font-bold">{t('preferences.title')}</h1>
        <p className="text-muted-foreground mt-1 text-sm">{t('preferences.subtitle')}</p>
      </div>

      <div className="grid [grid-template-columns:repeat(auto-fit,minmax(320px,1fr))] gap-[18px]">
        {/* Appearance */}
        <Card className="flex flex-col gap-4 p-[22px]">
          <div className="text-[15px] font-bold">{t('preferences.appearance')}</div>

          <div>
            <div className="text-muted-foreground mb-2 text-[11px] font-semibold uppercase">
              {t('preferences.theme')}
            </div>
            <div className="grid grid-cols-3 gap-2">
              {(['light', 'dark', 'system'] as ThemeChoice[]).map((choice) => (
                <button
                  key={choice}
                  type="button"
                  onClick={() => {
                    setTheme(choice);
                  }}
                  className={cn(
                    'flex flex-col items-center gap-1.5 rounded-[14px] border p-2.5 text-[11px] font-semibold transition-colors',
                    theme === choice
                      ? 'border-primary bg-accent text-accent-foreground'
                      : 'border-border text-text-2 hover:text-foreground',
                  )}
                >
                  <span
                    className="border-border size-[34px] rounded-[10px] border"
                    style={{
                      background:
                        choice === 'light'
                          ? '#ffffff'
                          : choice === 'dark'
                            ? 'hsl(247 10% 18%)'
                            : 'linear-gradient(135deg, #ffffff 50%, hsl(247 10% 18%) 50%)',
                    }}
                  />
                  {t(`preferences.theme_${choice}`)}
                </button>
              ))}
            </div>
          </div>

          <div className="border-border border-t pt-4">
            <div className="text-muted-foreground mb-2 text-[11px] font-semibold uppercase">
              {t('preferences.language')}
            </div>
            <SegmentedControl
              label={t('preferences.language')}
              value={i18n.language === 'pl' ? 'pl' : 'en'}
              onChange={(lng) => {
                void i18n.changeLanguage(lng);
              }}
              options={[
                { value: 'en', label: 'English' },
                { value: 'pl', label: 'Polski' },
              ]}
            />
          </div>

          <SwitchRow
            label={t('preferences.compact_density')}
            hint={t('preferences.compact_density_hint')}
            checked={prefs.compactDensity}
            onChange={(v) => {
              set('compactDensity', v);
            }}
          />
        </Card>

        {/* Units & formats */}
        <Card className="flex flex-col gap-4 p-[22px]">
          <div className="text-[15px] font-bold">{t('preferences.units')}</div>
          <div className="grid [grid-template-columns:repeat(auto-fit,minmax(140px,1fr))] gap-3">
            <SelectRow
              label={t('preferences.energy')}
              value={prefs.energyUnit}
              options={['kcal', 'kJ']}
              onChange={(v) => {
                set('energyUnit', v as Prefs['energyUnit']);
              }}
            />
            <SelectRow
              label={t('preferences.weight')}
              value={prefs.weightUnit}
              options={['kg', 'lb']}
              onChange={(v) => {
                set('weightUnit', v as Prefs['weightUnit']);
              }}
            />
            <SelectRow
              label={t('preferences.volume')}
              value={prefs.volumeUnit}
              options={['ml', 'L', 'oz']}
              onChange={(v) => {
                set('volumeUnit', v as Prefs['volumeUnit']);
              }}
            />
            <SelectRow
              label={t('preferences.week_start')}
              value={prefs.weekStart}
              options={['monday', 'sunday']}
              onChange={(v) => {
                set('weekStart', v as Prefs['weekStart']);
              }}
              display={(v) => t(`preferences.week_${v}`)}
            />
          </div>
          <SwitchRow
            label={t('preferences.thin_space')}
            hint={t('preferences.thin_space_hint')}
            checked={prefs.thinSpaceThousands}
            onChange={(v) => {
              set('thinSpaceThousands', v);
            }}
          />
          <Banner variant="info">{t('preferences.units_note')}</Banner>
        </Card>

        {/* Account */}
        <Card className="flex flex-col gap-3 p-[22px]">
          <div className="text-[15px] font-bold">{t('preferences.account')}</div>
          <div className="border-border bg-secondary flex items-center gap-3 rounded-[16px] border p-3.5">
            <span
              className="grid size-11 flex-none place-items-center rounded-[13px] text-[14px] font-bold text-white"
              style={{ background: 'var(--gradient-avatar)' }}
            >
              {getInitials(displayName)}
            </span>
            <div className="min-w-0">
              <div className="truncate text-[13.5px] font-bold">{displayName}</div>
              {profile?.email ? (
                <div className="text-muted-foreground truncate text-[11.5px]">{profile.email}</div>
              ) : null}
              {provider ? (
                <div className="text-muted-foreground truncate text-[11px]">{provider}</div>
              ) : null}
            </div>
          </div>

          <button
            type="button"
            onClick={() => {
              void auth.signoutRedirect();
            }}
            className="border-border hover:bg-muted flex items-center justify-between rounded-[13px] border px-3.5 py-3 text-[13px] font-semibold transition-colors"
          >
            {t('preferences.sign_out')}
            <LogOut className="size-4" />
          </button>

          <div
            className="flex items-center justify-between rounded-[13px] border px-3.5 py-3 text-[13px] font-semibold"
            style={{
              borderColor: 'color-mix(in oklab, var(--color-fat) 40%, transparent)',
              background: 'color-mix(in oklab, var(--color-fat) 8%, transparent)',
            }}
          >
            <span className="text-destructive">{t('preferences.delete_account')}</span>
            <Button size="xs" variant="destructive" disabled>
              {t('preferences.delete_account_btn')}
            </Button>
          </div>
        </Card>
      </div>
    </div>
  );
}

function SwitchRow({
  label,
  hint,
  checked,
  onChange,
}: {
  label: string;
  hint?: string;
  checked: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <div className="border-border flex items-center justify-between gap-3 border-t pt-4">
      <div className="min-w-0">
        <div className="text-[13px] font-semibold">{label}</div>
        {hint ? <div className="text-muted-foreground text-[11.5px]">{hint}</div> : null}
      </div>
      <Switch checked={checked} onCheckedChange={onChange} aria-label={label} />
    </div>
  );
}

function SelectRow({
  label,
  value,
  options,
  onChange,
  display,
}: {
  label: string;
  value: string;
  options: string[];
  onChange: (v: string) => void;
  display?: (v: string) => string;
}) {
  return (
    <label className="text-text-2 text-[11px] font-semibold uppercase">
      {label}
      <select
        value={value}
        onChange={(e) => {
          onChange(e.target.value);
        }}
        className="border-border bg-secondary text-foreground mt-1 h-[42px] w-full rounded-[13px] border px-3 text-[13px] font-medium normal-case outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]"
      >
        {options.map((o) => (
          <option key={o} value={o}>
            {display ? display(o) : o}
          </option>
        ))}
      </select>
    </label>
  );
}
