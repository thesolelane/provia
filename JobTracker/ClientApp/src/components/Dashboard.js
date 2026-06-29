import React, { useState, useEffect, useCallback, useRef } from 'react';
import { Link } from 'react-router-dom';
import { apiService } from '../services/apiService';

const REVENUE_POLL_INTERVAL = 60_000;

function RevenueTrendChart({ data }) {
  const [tooltip, setTooltip] = useState(null);

  if (!data || data.length === 0) return null;

  const maxVal = Math.max(...data.map(d => d.total), 1);
  const svgWidth = 480;
  const svgHeight = 120;
  const padLeft = 8;
  const padRight = 8;
  const padTop = 12;
  const padBottom = 32;
  const chartW = svgWidth - padLeft - padRight;
  const chartH = svgHeight - padTop - padBottom;
  const barCount = data.length;
  const gap = 10;
  const barW = (chartW - gap * (barCount - 1)) / barCount;

  const formatK = (v) => {
    if (v === 0) return '$0';
    if (v >= 1000) return `$${(v / 1000).toFixed(v % 1000 === 0 ? 0 : 1)}k`;
    return `$${v}`;
  };

  const formatFull = (v) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(v);

  const currentMonth = new Date().getMonth() + 1;
  const currentYear = new Date().getFullYear();

  return (
    <div style={{ position: 'relative', width: '100%', maxWidth: svgWidth }}>
      <svg
        viewBox={`0 0 ${svgWidth} ${svgHeight}`}
        style={{ width: '100%', height: 'auto', display: 'block', overflow: 'visible' }}
        aria-label="Revenue trend chart"
      >
        {data.map((d, i) => {
          const x = padLeft + i * (barW + gap);
          const barH = d.total === 0 ? 2 : Math.max(4, (d.total / maxVal) * chartH);
          const y = padTop + chartH - barH;
          const isCurrent = d.month === currentMonth && d.year === currentYear;
          const fill = isCurrent ? '#FF9500' : '#2F5A7E';
          const fillLight = isCurrent ? '#FF950033' : '#2F5A7E22';

          return (
            <g key={`${d.year}-${d.month}`}
              onMouseEnter={() => setTooltip({ i, label: d.label, total: d.total })}
              onMouseLeave={() => setTooltip(null)}
              style={{ cursor: 'default' }}
            >
              <rect
                x={x}
                y={padTop}
                width={barW}
                height={chartH}
                fill={fillLight}
                rx={4}
              />
              <rect
                x={x}
                y={y}
                width={barW}
                height={barH}
                fill={fill}
                rx={4}
                opacity={tooltip && tooltip.i !== i ? 0.55 : 1}
                style={{ transition: 'opacity 0.15s' }}
              />
              <text
                x={x + barW / 2}
                y={svgHeight - padBottom + 14}
                textAnchor="middle"
                fontSize={10}
                fill={isCurrent ? '#FF9500' : '#555'}
                fontWeight={isCurrent ? 700 : 400}
              >
                {d.label.split(' ')[0]}
              </text>
              <text
                x={x + barW / 2}
                y={svgHeight - padBottom + 25}
                textAnchor="middle"
                fontSize={9}
                fill="#999"
              >
                {d.label.split(' ')[1]}
              </text>
              {d.total > 0 && (
                <text
                  x={x + barW / 2}
                  y={y - 3}
                  textAnchor="middle"
                  fontSize={9}
                  fill={fill}
                  fontWeight={600}
                >
                  {formatK(d.total)}
                </text>
              )}
            </g>
          );
        })}
      </svg>

      {tooltip && (
        <div style={{
          position: 'absolute',
          top: 0,
          left: `${((tooltip.i + 0.5) / data.length) * 100}%`,
          transform: 'translate(-50%, -110%)',
          background: '#1a2b3c',
          color: '#fff',
          padding: '5px 10px',
          borderRadius: 6,
          fontSize: '0.78rem',
          fontWeight: 600,
          pointerEvents: 'none',
          whiteSpace: 'nowrap',
          zIndex: 10,
          boxShadow: '0 2px 8px rgba(0,0,0,0.25)',
        }}>
          {tooltip.label}: {formatFull(tooltip.total)}
        </div>
      )}
    </div>
  );
}

function Dashboard() {
  const [recentJobs, setRecentJobs] = useState([]);
  const [jobStats, setJobStats] = useState({
    total: 0,
    inProgress: 0,
    completed: 0,
    delayed: 0
  });
  const [invoiceSummary, setInvoiceSummary] = useState(null);
  const [revenueTrend, setRevenueTrend] = useState(null);
  const [loading, setLoading] = useState(true);
  const [revenueRefreshing, setRevenueRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const pollTimer = useRef(null);

  const fetchRevenueSummary = useCallback(async ({ silent = false } = {}) => {
    if (!silent) setRevenueRefreshing(true);
    try {
      const [summaryData, trendData] = await Promise.all([
        apiService.invoices.getSummary(),
        apiService.invoices.getRevenueTrend().catch(() => null),
      ]);
      setInvoiceSummary(summaryData);
      if (trendData) setRevenueTrend(trendData);
    } catch {
    } finally {
      if (!silent) setRevenueRefreshing(false);
    }
  }, []);

  const fetchJobStats = useCallback(async () => {
    try {
      const jobsData = await apiService.jobs.getAll();
      const sortedJobs = [...jobsData].sort((a, b) =>
        new Date(b.createdAt) - new Date(a.createdAt)
      ).slice(0, 5);
      setRecentJobs(sortedJobs);
      setJobStats({
        total: jobsData.length,
        inProgress: jobsData.filter(job => job.status.toLowerCase() === 'in progress').length,
        completed: jobsData.filter(job => job.status.toLowerCase() === 'completed').length,
        delayed: jobsData.filter(job => job.status.toLowerCase() === 'delayed').length,
      });
    } catch {
    }
  }, []);

  const startPolling = useCallback(() => {
    if (pollTimer.current) clearInterval(pollTimer.current);
    pollTimer.current = setInterval(() => {
      fetchRevenueSummary({ silent: true });
      fetchJobStats();
    }, REVENUE_POLL_INTERVAL);
  }, [fetchRevenueSummary, fetchJobStats]);

  useEffect(() => {
    fetchDashboardData();

    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        fetchRevenueSummary({ silent: true });
        fetchJobStats();
        startPolling();
      } else {
        if (pollTimer.current) clearInterval(pollTimer.current);
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      if (pollTimer.current) clearInterval(pollTimer.current);
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const fetchDashboardData = async () => {
    try {
      setLoading(true);
      const [jobsData, summaryData, trendData] = await Promise.all([
        apiService.jobs.getAll(),
        apiService.invoices.getSummary().catch(() => null),
        apiService.invoices.getRevenueTrend().catch(() => null),
      ]);

      const sortedJobs = [...jobsData].sort((a, b) =>
        new Date(b.createdAt) - new Date(a.createdAt)
      ).slice(0, 5);

      setRecentJobs(sortedJobs);

      setJobStats({
        total: jobsData.length,
        inProgress: jobsData.filter(job => job.status.toLowerCase() === 'in progress').length,
        completed: jobsData.filter(job => job.status.toLowerCase() === 'completed').length,
        delayed: jobsData.filter(job => job.status.toLowerCase() === 'delayed').length
      });

      setInvoiceSummary(summaryData);
      if (trendData) setRevenueTrend(trendData);
      setError(null);
      startPolling();
    } catch (err) {
      setError('Failed to load dashboard data. Please try again later.');
      console.error('Error fetching dashboard data:', err);
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount) => {
    if (amount == null) return '$0.00';
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);
  };

  if (loading) {
    return <div>Loading dashboard...</div>;
  }

  if (error) {
    return <div className="alert alert-danger">{error}</div>;
  }

  return (
    <div>
      <h2>Dashboard</h2>

      {/* Job Stats Cards */}
      <div className="grid">
        <div className="card">
          <h3>Total Jobs</h3>
          <p className="dashboard-stat">{jobStats.total}</p>
          <Link to="/jobs">View All Jobs</Link>
        </div>

        <div className="card">
          <h3>In Progress</h3>
          <p className="dashboard-stat">{jobStats.inProgress}</p>
        </div>

        <div className="card">
          <h3>Completed</h3>
          <p className="dashboard-stat">{jobStats.completed}</p>
        </div>

        <div className="card">
          <h3>Delayed</h3>
          <p className="dashboard-stat">{jobStats.delayed}</p>
        </div>
      </div>

      {/* Revenue Summary Widget */}
      {invoiceSummary && (
        <div className="card" style={{ marginBottom: '1.5rem' }}>
          <div className="card-header">
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem' }}>
              <h3 className="card-title" style={{ margin: 0 }}>Revenue Summary</h3>
              <button
                onClick={() => fetchRevenueSummary()}
                disabled={revenueRefreshing}
                title="Refresh revenue data"
                style={refreshBtnStyle(revenueRefreshing)}
                aria-label="Refresh revenue summary"
              >
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  width="14"
                  height="14"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  style={{ animation: revenueRefreshing ? 'provia-spin 0.8s linear infinite' : 'none' }}
                >
                  <polyline points="23 4 23 10 17 10" />
                  <polyline points="1 20 1 14 7 14" />
                  <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15" />
                </svg>
              </button>
            </div>
            <Link to="/invoices" className="btn btn-primary">View Invoices</Link>
          </div>
          <div className="grid" style={{ gridTemplateColumns: 'repeat(3, 1fr)', gap: '1rem', padding: '1rem 0' }}>
            <div style={summaryCardStyle('#fff8f0', '#FF9500')}>
              <div style={summaryLabelStyle}>Outstanding</div>
              <div style={summaryValueStyle('#FF9500')}>
                {formatCurrency(invoiceSummary.totalOutstanding)}
              </div>
              <Link
                to="/invoices?status=sent"
                style={summaryLinkStyle}
              >
                View sent &amp; overdue →
              </Link>
            </div>

            <div style={summaryCardStyle('#f0f8f4', '#2F5A7E')}>
              <div style={summaryLabelStyle}>Collected This Month</div>
              <div style={summaryValueStyle('#2F5A7E')}>
                {formatCurrency(invoiceSummary.paidThisMonth)}
              </div>
              <Link
                to="/invoices?status=paid"
                style={summaryLinkStyle}
              >
                View paid →
              </Link>
            </div>

            <div style={invoiceSummary.overdueCount > 0
              ? summaryCardStyle('#fff2f2', '#c0392b')
              : summaryCardStyle('#f5f5f5', '#666')}>
              <div style={summaryLabelStyle}>Overdue Invoices</div>
              <div style={summaryValueStyle(invoiceSummary.overdueCount > 0 ? '#c0392b' : '#666')}>
                {invoiceSummary.overdueCount}
              </div>
              <Link
                to="/invoices?status=overdue"
                style={summaryLinkStyle}
              >
                View overdue →
              </Link>
            </div>
          </div>

          {/* Revenue Trend Chart */}
          {revenueTrend && revenueTrend.length > 0 && (
            <div style={{ borderTop: '1px solid #eee', paddingTop: '1rem', marginTop: '0.25rem' }}>
              <div style={{ fontSize: '0.78rem', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.04em', color: '#666', marginBottom: '0.6rem' }}>
                Collected Revenue — Last 6 Months
              </div>
              <RevenueTrendChart data={revenueTrend} />
            </div>
          )}
        </div>
      )}

      {/* Recent Jobs */}
      <div className="card">
        <div className="card-header">
          <h3 className="card-title">Recent Jobs</h3>
          <Link to="/jobs" className="btn btn-primary">View All</Link>
        </div>

        {recentJobs.length === 0 ? (
          <p>No jobs found. Get started by creating your first job.</p>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Job Number</th>
                <th>Name</th>
                <th>Status</th>
                <th>Client</th>
              </tr>
            </thead>
            <tbody>
              {recentJobs.map(job => (
                <tr key={job.id}>
                  <td>{job.jobNumber}</td>
                  <td>
                    <Link to={`/jobs/${job.id}`}>{job.name}</Link>
                  </td>
                  <td>{job.status}</td>
                  <td>{job.clientName}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

const summaryCardStyle = (bg, borderColor) => ({
  background: bg,
  border: `1px solid ${borderColor}22`,
  borderRadius: '8px',
  padding: '1rem 1.25rem',
  display: 'flex',
  flexDirection: 'column',
  gap: '0.35rem',
});

const summaryLabelStyle = {
  fontSize: '0.78rem',
  fontWeight: 600,
  textTransform: 'uppercase',
  letterSpacing: '0.04em',
  color: '#666',
};

const summaryValueStyle = (color) => ({
  fontSize: '1.6rem',
  fontWeight: 700,
  color,
  lineHeight: 1.2,
});

const summaryLinkStyle = {
  fontSize: '0.8rem',
  color: '#2F5A7E',
  textDecoration: 'none',
  marginTop: '0.25rem',
};

const refreshBtnStyle = (spinning) => ({
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  background: 'none',
  border: '1px solid #ddd',
  borderRadius: '50%',
  width: '26px',
  height: '26px',
  cursor: spinning ? 'default' : 'pointer',
  color: spinning ? '#aaa' : '#2F5A7E',
  padding: 0,
  transition: 'color 0.2s, border-color 0.2s',
  flexShrink: 0,
});

if (typeof document !== 'undefined' && !document.getElementById('provia-spin-style')) {
  const style = document.createElement('style');
  style.id = 'provia-spin-style';
  style.textContent = '@keyframes provia-spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }';
  document.head.appendChild(style);
}

export default Dashboard;
