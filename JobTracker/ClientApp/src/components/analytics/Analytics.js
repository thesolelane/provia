import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { apiService } from '../../services/apiService';

const fmt = (n) => n == null ? '$0' : '$' + Number(n).toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
const fmtK = (n) => {
  if (!n) return '$0';
  if (Math.abs(n) >= 1000000) return '$' + (n / 1000000).toFixed(1) + 'M';
  if (Math.abs(n) >= 1000)    return '$' + (n / 1000).toFixed(1) + 'k';
  return '$' + Number(n).toFixed(0);
};
const pct = (n) => (n == null ? '0' : Number(n).toFixed(1)) + '%';

const JOB_COLORS = {
  Planning:          '#6c757d',
  PermitsPending:    '#fd7e14',
  InProgress:        '#0d6efd',
  InspectionPending: '#6f42c1',
  OnHold:            '#ffc107',
  Completed:         '#198754',
  Cancelled:         '#dc3545',
};

const STATUS_COLORS = {
  draft:   '#adb5bd',
  sent:    '#0d6efd',
  partial: '#fd7e14',
  paid:    '#198754',
  overdue: '#dc3545',
};

const CAT_COLORS = ['#2F5A7E','#FF9500','#198754','#6f42c1','#0d6efd','#fd7e14','#20c997','#dc3545'];

// ── Inline SVG bar chart ──────────────────────────────────────────────────────
function BarChart({ data, valueKey, labelKey, colorFn, height = 160, formatValue = fmtK }) {
  const [hovered, setHovered] = useState(null);
  if (!data?.length) return <div style={{ color: '#aaa', textAlign: 'center', padding: '2rem' }}>No data</div>;
  const maxVal = Math.max(...data.map(d => d[valueKey] || 0), 1);
  const BAR_W = Math.max(20, Math.min(52, Math.floor(560 / data.length) - 8));
  const SVG_W = data.length * (BAR_W + 8) + 20;

  return (
    <div style={{ overflowX: 'auto' }}>
      <svg width={SVG_W} height={height + 36} style={{ display: 'block' }}>
        {data.map((d, i) => {
          const val    = d[valueKey] || 0;
          const barH   = Math.max(2, Math.round((val / maxVal) * height));
          const x      = 10 + i * (BAR_W + 8);
          const y      = height - barH;
          const color  = colorFn ? colorFn(d, i) : CAT_COLORS[i % CAT_COLORS.length];
          const isHov  = hovered === i;
          return (
            <g key={i} onMouseEnter={() => setHovered(i)} onMouseLeave={() => setHovered(null)}>
              {/* ghost bar */}
              <rect x={x} y={0} width={BAR_W} height={height} fill="#f0f0f0" rx={3} />
              {/* value bar */}
              <rect x={x} y={y} width={BAR_W} height={barH} fill={color} rx={3} opacity={isHov ? 1 : 0.85} />
              {/* tooltip */}
              {isHov && (
                <text x={x + BAR_W / 2} y={Math.max(14, y - 4)} textAnchor="middle" fontSize={10} fontWeight={700} fill={color}>
                  {formatValue(val)}
                </text>
              )}
              {/* x-axis label */}
              <text x={x + BAR_W / 2} y={height + 16} textAnchor="middle" fontSize={9} fill="#6c757d">
                {(d[labelKey] || '').slice(0, 6)}
              </text>
            </g>
          );
        })}
      </svg>
    </div>
  );
}

// ── Horizontal bar (pipeline / funnel) ───────────────────────────────────────
function HBar({ label, value, max, color, sublabel }) {
  const pct = max > 0 ? Math.max(2, (value / max) * 100) : 0;
  return (
    <div style={{ marginBottom: '0.6rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.78rem', marginBottom: '0.25rem' }}>
        <span style={{ fontWeight: 600, color: '#495057' }}>{label}</span>
        <span style={{ fontWeight: 700, color }}>{value}{sublabel ? <span style={{ fontWeight: 400, color: '#aaa', fontSize: '0.72rem' }}> {sublabel}</span> : ''}</span>
      </div>
      <div style={{ background: '#f0f0f0', borderRadius: 4, height: 10, overflow: 'hidden' }}>
        <div style={{ width: `${pct}%`, background: color, height: '100%', borderRadius: 4, transition: 'width 0.4s ease' }} />
      </div>
    </div>
  );
}

// ── Overview card ─────────────────────────────────────────────────────────────
function OCard({ label, value, sub, color, link, note, noteColor }) {
  const inner = (
    <div className="card" style={{ padding: '1rem 1.25rem', cursor: link ? 'pointer' : 'default', transition: 'box-shadow 0.15s' }}
      onMouseEnter={e => link && (e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.1)')}
      onMouseLeave={e => link && (e.currentTarget.style.boxShadow = '')}>
      <div style={{ fontSize: '0.72rem', fontWeight: 700, color: '#6c757d', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '0.35rem' }}>{label}</div>
      <div style={{ fontSize: '1.6rem', fontWeight: 800, color, lineHeight: 1.1 }}>{value}</div>
      {sub   && <div style={{ fontSize: '0.78rem', color: '#6c757d', marginTop: '0.25rem' }}>{sub}</div>}
      {note  && <div style={{ fontSize: '0.75rem', fontWeight: 600, color: noteColor || '#198754', marginTop: '0.25rem' }}>{note}</div>}
    </div>
  );
  return link ? <Link to={link} style={{ textDecoration: 'none' }}>{inner}</Link> : inner;
}

// ── Section card ─────────────────────────────────────────────────────────────
function Section({ title, children, span = 1 }) {
  return (
    <div className="card" style={{ padding: '1.25rem', gridColumn: span > 1 ? `span ${span}` : undefined }}>
      <h6 style={{ margin: '0 0 1rem', fontWeight: 700, fontSize: '0.85rem', textTransform: 'uppercase', letterSpacing: '0.5px', color: '#495057' }}>{title}</h6>
      {children}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
export default function Analytics() {
  const [overview, setOverview]   = useState(null);
  const [jobs,     setJobs]       = useState(null);
  const [leads,    setLeads]      = useState(null);
  const [revenue,  setRevenue]    = useState(null);
  const [vendors,  setVendors]    = useState(null);
  const [loading,  setLoading]    = useState(true);
  const [tab,      setTab]        = useState('overview'); // overview | jobs | leads | revenue | vendors

  useEffect(() => {
    (async () => {
      setLoading(true);
      try {
        const [ov, jb, ld, rv, vd] = await Promise.all([
          apiService.analytics.getOverview(),
          apiService.analytics.getJobs(),
          apiService.analytics.getLeads(),
          apiService.analytics.getRevenue(),
          apiService.analytics.getVendors(),
        ]);
        setOverview(ov); setJobs(jb); setLeads(ld); setRevenue(rv); setVendors(vd);
      } catch (e) { console.error(e); }
      finally { setLoading(false); }
    })();
  }, []);

  const tabs = [
    { id: 'overview', label: 'Overview'  },
    { id: 'jobs',     label: 'Jobs'      },
    { id: 'leads',    label: 'Leads'     },
    { id: 'revenue',  label: 'Revenue'   },
    { id: 'vendors',  label: 'Vendors'   },
  ];

  return (
    <div>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <div>
          <h2 style={{ margin: 0 }}>Analytics</h2>
          <p style={{ margin: '0.25rem 0 0', color: '#6c757d', fontSize: '0.9rem' }}>Business performance at a glance</p>
        </div>
        <div style={{ fontSize: '0.78rem', color: '#aaa' }}>All data for your company · updated live</div>
      </div>

      {/* Tab bar */}
      <div style={{ display: 'flex', gap: '0.25rem', marginBottom: '1.5rem', borderBottom: '2px solid #dee2e6', paddingBottom: '0' }}>
        {tabs.map(t => (
          <button key={t.id} onClick={() => setTab(t.id)} style={{
            padding: '0.5rem 1.1rem', border: 'none', background: 'none', cursor: 'pointer',
            fontWeight: 700, fontSize: '0.85rem',
            color: tab === t.id ? '#2F5A7E' : '#6c757d',
            borderBottom: tab === t.id ? '3px solid #2F5A7E' : '3px solid transparent',
            marginBottom: '-2px',
          }}>{t.label}</button>
        ))}
      </div>

      {loading ? (
        <div style={{ padding: '4rem', textAlign: 'center', color: '#6c757d' }}>Loading analytics...</div>
      ) : (
        <>
          {/* ── OVERVIEW ─────────────────────────────────────────────────── */}
          {tab === 'overview' && overview && (
            <div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '1rem', marginBottom: '1.25rem' }}>
                <OCard label="Active Jobs"       value={overview.activeJobs}       color="#0d6efd" sub={`${overview.totalJobs} total · ${overview.completedJobs} completed`} link="/jobs" />
                <OCard label="Revenue This Month" value={fmt(overview.revenueThisMonth)} color="#FF9500" sub={`vs ${fmt(overview.revenueLastMonth)} last month`}
                  note={overview.revenueMoMChange >= 0 ? `▲ ${overview.revenueMoMChange}% MoM` : `▼ ${Math.abs(overview.revenueMoMChange)}% MoM`}
                  noteColor={overview.revenueMoMChange >= 0 ? '#198754' : '#dc3545'} link="/invoices" />
                <OCard label="Outstanding"       value={fmt(overview.outstanding)}  color="#2F5A7E" sub="sent + partial invoices" link="/invoices" />
                <OCard label="Lead Pipeline"     value={overview.activeLeads}       color="#6f42c1" sub={`${overview.totalLeads} total · ${pct(overview.conversionRate)} converted`} link="/leads" />
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '1rem', marginBottom: '1.25rem' }}>
                <OCard label="Signed / Won Leads" value={overview.convertedLeads}  color="#198754" link="/leads" />
                <OCard label="Active Vendors"     value={overview.activeVendors}    color="#fd7e14" link="/vendors" />
                <OCard label="Field Photos"       value={overview.totalPhotos}      color="#20c997" link="/photos" />
                <OCard label="Collection Rate"    value={pct(overview.collectionRate)} color="#2F5A7E" sub="paid ÷ total invoiced" />
              </div>

              {/* Quick charts side-by-side */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <Section title="Job Pipeline">
                  {jobs?.pipeline?.map(p => (
                    <HBar key={p.status} label={p.label} value={p.count}
                      max={Math.max(...(jobs.pipeline.map(x => x.count)), 1)}
                      color={JOB_COLORS[p.status] || '#6c757d'} />
                  ))}
                </Section>
                <Section title="Revenue — Last 12 Months">
                  {revenue?.trend && (
                    <BarChart data={revenue.trend} valueKey="collected" labelKey="label"
                      colorFn={(d, i) => i === revenue.trend.length - 1 ? '#FF9500' : '#2F5A7E'} height={140} />
                  )}
                </Section>
              </div>
            </div>
          )}

          {/* ── JOBS ─────────────────────────────────────────────────────── */}
          {tab === 'jobs' && jobs && (
            <div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '1rem', marginBottom: '1.25rem' }}>
                <OCard label="Total Jobs" value={jobs.pipeline.reduce((s, p) => s + p.count, 0)} color="#2F5A7E" />
                <OCard label="Avg Days to Complete" value={jobs.avgDaysToComplete ? `${jobs.avgDaysToComplete}d` : '—'} color="#6f42c1" sub="for completed jobs" />
                <OCard label="Active Right Now" value={jobs.pipeline.filter(p => ['InProgress','InspectionPending','PermitsPending'].includes(p.status)).reduce((s, p) => s + p.count, 0)} color="#0d6efd" />
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <Section title="Pipeline by Status">
                  {jobs.pipeline.map(p => (
                    <HBar key={p.status} label={p.label} value={p.count}
                      max={Math.max(...jobs.pipeline.map(x => x.count), 1)}
                      color={JOB_COLORS[p.status] || '#6c757d'} sublabel={p.count === 1 ? 'job' : 'jobs'} />
                  ))}
                </Section>
                <Section title="New Jobs per Month">
                  <BarChart data={jobs.createdByMonth} valueKey="count" labelKey="label"
                    colorFn={(d, i) => i === jobs.createdByMonth.length - 1 ? '#FF9500' : '#2F5A7E'}
                    height={160} formatValue={v => String(v)} />
                </Section>
              </div>
            </div>
          )}

          {/* ── LEADS ────────────────────────────────────────────────────── */}
          {tab === 'leads' && leads && (
            <div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '1rem', marginBottom: '1.25rem' }}>
                <OCard label="Total Leads"    value={leads.total}                          color="#6f42c1" />
                <OCard label="Won / Signed"   value={leads.stages.find(s => s.stage === 'signed')?.count ?? 0} color="#198754" />
                <OCard label="In Pipeline"    value={leads.stages.filter(s => s.stage !== 'signed').reduce((s, x) => s + x.count, 0)} color="#0d6efd" />
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <Section title="Lead Funnel by Stage">
                  {leads.stages.map((s, i) => (
                    <HBar key={s.stage} label={s.label} value={s.count}
                      max={Math.max(...leads.stages.map(x => x.count), 1)}
                      color={i === leads.stages.length - 1 ? '#198754' : '#6f42c1'}
                      sublabel={s.count === 1 ? 'lead' : 'leads'} />
                  ))}
                </Section>
                <Section title="New Leads per Month">
                  <BarChart data={leads.byMonth} valueKey="total" labelKey="label"
                    colorFn={(_, i) => i === leads.byMonth.length - 1 ? '#FF9500' : '#6f42c1'}
                    height={160} formatValue={v => String(v)} />
                  <div style={{ marginTop: '0.75rem', fontSize: '0.75rem', color: '#6c757d' }}>
                    Orange bar = signed leads · Purple = all leads
                  </div>
                </Section>
              </div>
            </div>
          )}

          {/* ── REVENUE ──────────────────────────────────────────────────── */}
          {tab === 'revenue' && revenue && (
            <div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '1rem', marginBottom: '1.25rem' }}>
                <OCard label="Total Invoiced"   value={fmt(revenue.totalInvoiced)}   color="#2F5A7E" />
                <OCard label="Total Collected"  value={fmt(revenue.totalCollected)}  color="#198754" />
                <OCard label="Collection Rate"  value={pct(revenue.collectionRate)}  color="#FF9500" sub="paid ÷ total invoiced" />
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <Section title="Collected — Last 12 Months" span={2}>
                  <BarChart data={revenue.trend} valueKey="collected" labelKey="label"
                    colorFn={(d, i) => i === revenue.trend.length - 1 ? '#FF9500' : '#2F5A7E'}
                    height={160} />
                </Section>
                <Section title="Invoice Status Breakdown">
                  {revenue.statusBreakdown.map(s => (
                    <HBar key={s.status} label={s.label} value={s.count}
                      max={Math.max(...revenue.statusBreakdown.map(x => x.count), 1)}
                      color={STATUS_COLORS[s.status] || '#6c757d'}
                      sublabel={`· ${fmt(s.total)}`} />
                  ))}
                </Section>
                <Section title="Invoiced vs Collected by Month">
                  {revenue.trend.slice(-6).map((m, i) => (
                    <div key={i} style={{ marginBottom: '0.5rem' }}>
                      <div style={{ fontSize: '0.75rem', fontWeight: 600, color: '#6c757d', marginBottom: '0.2rem' }}>{m.label}</div>
                      <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                        <div style={{ flex: 1, background: '#f0f0f0', borderRadius: 3, height: 8, overflow: 'hidden' }}>
                          <div style={{ height: '100%', background: '#2F5A7E', borderRadius: 3,
                            width: revenue.trend.slice(-6).reduce((mx, x) => Math.max(mx, x.invoiced), 1) > 0
                              ? `${(m.invoiced / revenue.trend.slice(-6).reduce((mx, x) => Math.max(mx, x.invoiced), 1)) * 100}%` : '0%' }} />
                        </div>
                        <span style={{ fontSize: '0.7rem', color: '#2F5A7E', fontWeight: 700, minWidth: 56, textAlign: 'right' }}>{fmtK(m.invoiced)}</span>
                      </div>
                      <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', marginTop: '0.15rem' }}>
                        <div style={{ flex: 1, background: '#f0f0f0', borderRadius: 3, height: 8, overflow: 'hidden' }}>
                          <div style={{ height: '100%', background: '#198754', borderRadius: 3,
                            width: revenue.trend.slice(-6).reduce((mx, x) => Math.max(mx, x.invoiced), 1) > 0
                              ? `${(m.collected / revenue.trend.slice(-6).reduce((mx, x) => Math.max(mx, x.invoiced), 1)) * 100}%` : '0%' }} />
                        </div>
                        <span style={{ fontSize: '0.7rem', color: '#198754', fontWeight: 700, minWidth: 56, textAlign: 'right' }}>{fmtK(m.collected)}</span>
                      </div>
                    </div>
                  ))}
                  <div style={{ display: 'flex', gap: '1rem', marginTop: '0.75rem', fontSize: '0.72rem', color: '#6c757d' }}>
                    <span><span style={{ background: '#2F5A7E', display: 'inline-block', width: 10, height: 6, borderRadius: 2, marginRight: 4 }} />Invoiced</span>
                    <span><span style={{ background: '#198754', display: 'inline-block', width: 10, height: 6, borderRadius: 2, marginRight: 4 }} />Collected</span>
                  </div>
                </Section>
              </div>
            </div>
          )}

          {/* ── VENDORS ──────────────────────────────────────────────────── */}
          {tab === 'vendors' && vendors && (
            <div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '1rem', marginBottom: '1.25rem' }}>
                <OCard label="Total Spend"    value={fmt(vendors.totalSpend)}          color="#fd7e14" />
                <OCard label="Categories"     value={vendors.byCategory.length}         color="#6f42c1" />
                <OCard label="Top Vendors"    value={vendors.topVendors.length}          color="#2F5A7E" />
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <Section title="Spend by Category">
                  {vendors.byCategory.length === 0
                    ? <div style={{ color: '#aaa', textAlign: 'center', padding: '1.5rem' }}>No purchases recorded yet</div>
                    : vendors.byCategory.map((c, i) => (
                      <HBar key={c.category} label={c.category.charAt(0).toUpperCase() + c.category.slice(1)}
                        value={c.count} max={Math.max(...vendors.byCategory.map(x => x.count), 1)}
                        color={CAT_COLORS[i % CAT_COLORS.length]}
                        sublabel={`· ${fmt(c.total)}`} />
                    ))}
                </Section>
                <Section title="Top Vendors by Spend">
                  {vendors.topVendors.length === 0
                    ? <div style={{ color: '#aaa', textAlign: 'center', padding: '1.5rem' }}>No purchases recorded yet</div>
                    : vendors.topVendors.map((v, i) => (
                      <div key={v.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0.4rem 0', borderBottom: i < vendors.topVendors.length - 1 ? '1px solid #f0f0f0' : 'none' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                          <span style={{ width: 20, height: 20, borderRadius: '50%', background: CAT_COLORS[i % CAT_COLORS.length], display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#fff', fontSize: '0.65rem', fontWeight: 800, flexShrink: 0 }}>{i + 1}</span>
                          <Link to={`/vendors/${v.id}`} style={{ fontSize: '0.85rem', fontWeight: 600, color: '#212529', textDecoration: 'none' }}>{v.name}</Link>
                        </div>
                        <div style={{ textAlign: 'right' }}>
                          <div style={{ fontWeight: 700, color: '#fd7e14', fontSize: '0.85rem' }}>{fmt(v.total)}</div>
                          <div style={{ fontSize: '0.7rem', color: '#aaa' }}>{v.count} purchase{v.count !== 1 ? 's' : ''}</div>
                        </div>
                      </div>
                    ))}
                </Section>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
