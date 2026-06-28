import React, { useState, useEffect, useCallback, useRef } from 'react';
import { apiService } from '../../services/apiService';

const CATEGORIES = ['all', 'progress', 'issue', 'completion', 'material', 'safety', 'before', 'after', 'general'];

const CAT_META = {
  progress:   { color: '#0d6efd', bg: '#e7f0ff', label: 'Progress'  },
  issue:      { color: '#dc3545', bg: '#fdecea', label: 'Issue'      },
  completion: { color: '#198754', bg: '#e6f7ee', label: 'Completion' },
  material:   { color: '#fd7e14', bg: '#fff3e0', label: 'Material'   },
  safety:     { color: '#dc3545', bg: '#fdecea', label: 'Safety'     },
  before:     { color: '#6f42c1', bg: '#f0ebff', label: 'Before'     },
  after:      { color: '#20c997', bg: '#e0faf4', label: 'After'      },
  general:    { color: '#6c757d', bg: '#f8f9fa', label: 'General'    },
};

const ALL_EXT_OPTIONS = [
  { ext: 'jpg',  label: 'JPEG (.jpg)' },
  { ext: 'jpeg', label: 'JPEG (.jpeg)' },
  { ext: 'png',  label: 'PNG'  },
  { ext: 'webp', label: 'WebP' },
  { ext: 'heic', label: 'HEIC (iPhone)' },
  { ext: 'heif', label: 'HEIF' },
  { ext: 'gif',  label: 'GIF'  },
  { ext: 'bmp',  label: 'BMP'  },
  { ext: 'tiff', label: 'TIFF' },
];

function fmtBytes(b) {
  if (!b) return '0 B';
  if (b < 1024) return `${b} B`;
  if (b < 1024 * 1024) return `${(b / 1024).toFixed(0)} KB`;
  return `${(b / 1024 / 1024).toFixed(1)} MB`;
}
function fmtDate(d) {
  if (!d) return '';
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}
function fmtDateTime(d) {
  if (!d) return '';
  return new Date(d).toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: 'numeric', minute: '2-digit' });
}

export default function FieldPhotoGallery({ jobId = null }) {
  const [photos, setPhotos]               = useState([]);
  const [summary, setSummary]             = useState(null);
  const [loading, setLoading]             = useState(true);
  const [uploading, setUploading]         = useState(false);
  const [uploadProgress, setUploadProgress] = useState(null);
  const [category, setCategory]           = useState('all');
  const [search, setSearch]               = useState('');
  const [page, setPage]                   = useState(1);
  const [total, setTotal]                 = useState(0);
  const [lightbox, setLightbox]           = useState(null);
  const [editingId, setEditingId]         = useState(null);
  const [editForm, setEditForm]           = useState({});
  const [showSettings, setShowSettings]   = useState(false);
  const [settings, setSettings]           = useState(null);
  const [settingsForm, setSettingsForm]   = useState(null);
  const [settingsSaving, setSettingsSaving] = useState(false);
  const [settingsMsg, setSettingsMsg]     = useState(null);
  const fileInputRef = useRef();
  const PAGE_SIZE = 48;

  const fetchPhotos = useCallback(async () => {
    try {
      setLoading(true);
      const [res, sum] = await Promise.all([
        apiService.photos.getAll({ jobId, category, search, page, pageSize: PAGE_SIZE }),
        apiService.photos.getSummary(),
      ]);
      setPhotos(res.photos);
      setTotal(res.total);
      setSummary(sum);
    } catch (e) { console.error(e); }
    finally { setLoading(false); }
  }, [jobId, category, search, page]);

  useEffect(() => { setPage(1); }, [category, search]);
  useEffect(() => { fetchPhotos(); }, [fetchPhotos]);

  const loadSettings = async () => {
    try {
      const s = await apiService.photos.getSettings();
      setSettings(s);
      const exts = (s.allowedExtensions || 'jpg,jpeg,png,webp,heic,heif')
        .split(',').map(e => e.trim()).filter(Boolean);
      setSettingsForm({
        storagePath: s.storagePath || '',
        allowedExtensions: exts,
        maxFileSizeMb: s.maxFileSizeMb || 20,
        enableIpfs: s.enableIpfs || false,
      });
    } catch (e) { console.error(e); }
  };

  const openSettings = () => { setShowSettings(true); loadSettings(); };

  const saveSettings = async () => {
    setSettingsSaving(true);
    setSettingsMsg(null);
    try {
      const data = {
        storagePath: settingsForm.storagePath,
        allowedExtensions: settingsForm.allowedExtensions.join(','),
        maxFileSizeMb: parseInt(settingsForm.maxFileSizeMb, 10) || 20,
        enableIpfs: settingsForm.enableIpfs,
      };
      const res = await apiService.photos.saveSettings(data);
      setSettings(res);
      setSettingsMsg({ type: 'success', text: 'Settings saved!' });
    } catch (e) {
      setSettingsMsg({ type: 'error', text: e.message || 'Failed to save settings' });
    } finally { setSettingsSaving(false); }
  };

  const toggleExt = (ext) => {
    setSettingsForm(f => ({
      ...f,
      allowedExtensions: f.allowedExtensions.includes(ext)
        ? f.allowedExtensions.filter(e => e !== ext)
        : [...f.allowedExtensions, ext],
    }));
  };

  // Upload handler
  const handleFiles = async (files) => {
    if (!files?.length) return;
    setUploading(true);
    let done = 0;
    for (const file of Array.from(files)) {
      setUploadProgress(`Uploading ${done + 1} of ${files.length}: ${file.name}`);
      try {
        const form = new FormData();
        form.append('file', file);
        form.append('category', category !== 'all' ? category : 'general');
        if (jobId) form.append('jobId', String(jobId));
        await apiService.photos.upload(form);
        done++;
      } catch (e) { console.error('Upload failed for', file.name, e); }
    }
    setUploadProgress(null);
    setUploading(false);
    fetchPhotos();
  };

  const handleDrop = (e) => { e.preventDefault(); handleFiles(e.dataTransfer.files); };

  const handleDelete = async (id) => {
    if (!window.confirm('Delete this photo?')) return;
    try { await apiService.photos.delete(id); setLightbox(null); fetchPhotos(); }
    catch (e) { alert('Failed: ' + e.message); }
  };

  const startEdit = (photo) => {
    setEditingId(photo.id);
    setEditForm({ category: photo.category || 'general', caption: photo.caption || '', tags: photo.tags || '' });
  };

  const saveEdit = async (id) => {
    try {
      await apiService.photos.update(id, editForm);
      setEditingId(null);
      fetchPhotos();
      if (lightbox?.id === id) setLightbox(prev => ({ ...prev, ...editForm }));
    } catch (e) { alert('Failed: ' + e.message); }
  };

  const pages = Math.ceil(total / PAGE_SIZE);

  // ── Settings Panel ────────────────────────────────────────────────────
  if (showSettings) {
    return (
      <div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '1.5rem' }}>
          <button className="btn" onClick={() => { setShowSettings(false); setSettingsMsg(null); }} style={{ color: '#6c757d' }}>← Back</button>
          <div>
            <h2 style={{ margin: 0 }}>Photo Storage Settings</h2>
            <p style={{ margin: 0, color: '#6c757d', fontSize: '0.85rem' }}>Configure where and how photos are stored on your server</p>
          </div>
        </div>

        {!settingsForm ? (
          <div style={{ padding: '2rem', textAlign: 'center', color: '#999' }}>Loading settings...</div>
        ) : (
          <div style={{ maxWidth: 640 }}>

            {/* Storage Path */}
            <div className="card" style={{ padding: '1.25rem', marginBottom: '1rem' }}>
              <h5 style={{ margin: '0 0 0.75rem', fontSize: '0.95rem', fontWeight: 700 }}>📁 Storage Location</h5>
              <p style={{ fontSize: '0.82rem', color: '#6c757d', marginBottom: '0.75rem' }}>
                Where photos are saved on your Docker server. Leave blank to use the default uploads folder inside the app.
                Use an absolute path to save to a mounted volume or external drive (e.g. <code>/mnt/nas/photos</code>).
              </p>
              <input
                className="form-control"
                placeholder="Leave blank for default  —  or enter: /mnt/nas/photos"
                value={settingsForm.storagePath}
                onChange={e => setSettingsForm(f => ({ ...f, storagePath: e.target.value }))}
              />
              {settings?.effectiveStorageDir && (
                <div style={{ marginTop: '0.5rem', fontSize: '0.78rem', color: '#6c757d' }}>
                  Current effective path: <code style={{ background: '#f8f9fa', padding: '1px 4px', borderRadius: 3 }}>{settings.effectiveStorageDir}</code>
                </div>
              )}
            </div>

            {/* Allowed File Types */}
            <div className="card" style={{ padding: '1.25rem', marginBottom: '1rem' }}>
              <h5 style={{ margin: '0 0 0.75rem', fontSize: '0.95rem', fontWeight: 700 }}>🖼 Allowed File Types</h5>
              <p style={{ fontSize: '0.82rem', color: '#6c757d', marginBottom: '0.75rem' }}>
                Only these file formats can be uploaded. JPEG and PNG are recommended for best compatibility.
              </p>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
                {ALL_EXT_OPTIONS.map(({ ext, label }) => {
                  const on = settingsForm.allowedExtensions.includes(ext);
                  return (
                    <label key={ext} style={{
                      display: 'flex', alignItems: 'center', gap: '0.4rem',
                      padding: '6px 12px', borderRadius: 20, cursor: 'pointer',
                      border: `1px solid ${on ? '#2F5A7E' : '#dee2e6'}`,
                      background: on ? '#e8f0f8' : '#fff',
                      fontSize: '0.82rem', fontWeight: 600, userSelect: 'none',
                    }}>
                      <input type="checkbox" checked={on} onChange={() => toggleExt(ext)} style={{ display: 'none' }} />
                      {on ? '✓' : ''} {label}
                    </label>
                  );
                })}
              </div>
              {settingsForm.allowedExtensions.length === 0 && (
                <div style={{ marginTop: '0.5rem', fontSize: '0.8rem', color: '#dc3545' }}>Select at least one file type.</div>
              )}
            </div>

            {/* Max File Size */}
            <div className="card" style={{ padding: '1.25rem', marginBottom: '1rem' }}>
              <h5 style={{ margin: '0 0 0.75rem', fontSize: '0.95rem', fontWeight: 700 }}>📏 Max File Size per Upload</h5>
              <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                <input
                  type="range" min={1} max={200} step={1}
                  value={settingsForm.maxFileSizeMb}
                  onChange={e => setSettingsForm(f => ({ ...f, maxFileSizeMb: Number(e.target.value) }))}
                  style={{ flex: 1 }}
                />
                <div style={{ minWidth: 90 }}>
                  <input
                    type="number" min={1} max={200} className="form-control form-control-sm"
                    value={settingsForm.maxFileSizeMb}
                    onChange={e => setSettingsForm(f => ({ ...f, maxFileSizeMb: Math.min(200, Math.max(1, Number(e.target.value))) }))}
                    style={{ textAlign: 'center' }}
                  />
                </div>
                <span style={{ color: '#6c757d', fontSize: '0.85rem', minWidth: 24 }}>MB</span>
              </div>
              <div style={{ marginTop: '0.4rem', fontSize: '0.78rem', color: '#6c757d' }}>
                Range: 1 MB – 200 MB per file. iPhone HEIC photos are typically 3–6 MB; full-quality JPEGs 2–8 MB.
              </div>
            </div>

            {/* IPFS */}
            <div className="card" style={{ padding: '1.25rem', marginBottom: '1.5rem', opacity: settings?.pinataConfigured ? 1 : 0.6 }}>
              <h5 style={{ margin: '0 0 0.5rem', fontSize: '0.95rem', fontWeight: 700 }}>🌐 IPFS Storage (via Pinata)</h5>
              {!settings?.pinataConfigured ? (
                <div style={{ fontSize: '0.82rem', color: '#6c757d' }}>
                  To enable IPFS, add a <code>PINATA_JWT</code> environment variable with your Pinata API key.
                  Free tier at <a href="https://pinata.cloud" target="_blank" rel="noopener noreferrer">pinata.cloud</a> includes 1 GB.
                  Photos uploaded via IPFS are accessible from anywhere without needing your server.
                </div>
              ) : (
                <>
                  <p style={{ fontSize: '0.82rem', color: '#6c757d', marginBottom: '0.75rem' }}>
                    When enabled, each photo is also pinned to IPFS. The IPFS gateway URL is stored and the photo
                    is accessible even if your server is offline. Local copy is always kept as backup.
                  </p>
                  <label style={{ display: 'flex', alignItems: 'center', gap: '0.6rem', cursor: 'pointer' }}>
                    <input
                      type="checkbox"
                      checked={settingsForm.enableIpfs}
                      onChange={e => setSettingsForm(f => ({ ...f, enableIpfs: e.target.checked }))}
                    />
                    <span style={{ fontWeight: 600, fontSize: '0.9rem' }}>Pin new uploads to IPFS</span>
                    <span style={{ fontSize: '0.78rem', color: '#198754', fontWeight: 600 }}>✓ Pinata key configured</span>
                  </label>
                </>
              )}
            </div>

            {settingsMsg && (
              <div style={{
                padding: '0.75rem 1rem', borderRadius: 6, marginBottom: '1rem',
                background: settingsMsg.type === 'success' ? '#e6f7ee' : '#fdecea',
                color: settingsMsg.type === 'success' ? '#198754' : '#dc3545',
                fontWeight: 600, fontSize: '0.88rem',
              }}>
                {settingsMsg.text}
              </div>
            )}

            <div style={{ display: 'flex', gap: '0.75rem' }}>
              <button className="btn btn-primary" onClick={saveSettings}
                disabled={settingsSaving || settingsForm.allowedExtensions.length === 0}>
                {settingsSaving ? 'Saving...' : 'Save Settings'}
              </button>
              <button className="btn" onClick={() => { setShowSettings(false); setSettingsMsg(null); }}
                style={{ color: '#6c757d' }}>
                Cancel
              </button>
            </div>
          </div>
        )}
      </div>
    );
  }

  // ── Main Gallery ──────────────────────────────────────────────────────
  return (
    <div>
      {/* Header */}
      {!jobId && (
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <div>
            <h2 style={{ margin: 0 }}>Field Photos</h2>
            <p style={{ margin: '0.25rem 0 0', color: '#6c757d', fontSize: '0.9rem' }}>Construction site photo library</p>
          </div>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button className="btn" onClick={openSettings} style={{ color: '#6c757d', border: '1px solid #dee2e6' }} title="Storage settings">⚙ Settings</button>
            <button className="btn btn-primary" onClick={() => fileInputRef.current?.click()} disabled={uploading}>
              {uploading ? uploadProgress || 'Uploading...' : '+ Upload Photos'}
            </button>
          </div>
        </div>
      )}

      {/* Summary cards */}
      {summary && !jobId && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: '1rem', marginBottom: '1.5rem' }}>
          <SCard label="Total Photos"  value={summary.total}                    color="#2F5A7E" />
          <SCard label="This Week"     value={summary.thisWeek}                 color="#0d6efd" />
          <SCard label="Local Storage" value={summary.localCount}               color="#198754" />
          <SCard label="On IPFS"       value={summary.ipfsCount}                color="#6f42c1" />
          <SCard label="Storage Used"  value={fmtBytes(summary.totalSizeBytes)} color="#fd7e14" text />
        </div>
      )}

      {/* Drop zone for job context */}
      {jobId && (
        <div
          onDrop={handleDrop} onDragOver={e => e.preventDefault()}
          onClick={() => fileInputRef.current?.click()}
          style={{
            border: '2px dashed #dee2e6', borderRadius: 8, padding: '1.5rem', textAlign: 'center',
            cursor: 'pointer', marginBottom: '1rem', color: '#6c757d', fontSize: '0.9rem', background: '#fafafa',
          }}
        >
          {uploading
            ? <div style={{ color: '#0d6efd', fontWeight: 600 }}>{uploadProgress || 'Uploading...'}</div>
            : <>📷 Drop photos here or <strong>click to upload</strong> — uses your configured file type &amp; size limits</>}
        </div>
      )}

      <input ref={fileInputRef} type="file" accept="image/*" multiple style={{ display: 'none' }}
        onChange={e => handleFiles(e.target.files)} />

      {/* Filters */}
      <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', alignItems: 'center', marginBottom: '1rem' }}>
        <div style={{ display: 'flex', gap: '0.25rem', flexWrap: 'wrap' }}>
          {CATEGORIES.map(c => (
            <button key={c} onClick={() => setCategory(c)} style={{
              padding: '4px 12px', borderRadius: 20, border: '1px solid', cursor: 'pointer',
              fontSize: '0.8rem', fontWeight: 600, textTransform: 'capitalize',
              background: category === c ? '#2F5A7E' : '#fff',
              color: category === c ? '#fff' : '#6c757d',
              borderColor: category === c ? '#2F5A7E' : '#dee2e6',
            }}>{c === 'all' ? 'All' : c}</button>
          ))}
        </div>
        <input className="form-control" placeholder="Search caption, tags..." value={search}
          onChange={e => setSearch(e.target.value)} style={{ maxWidth: 240 }} />
        {!jobId && (
          <div
            onDrop={handleDrop} onDragOver={e => e.preventDefault()}
            onClick={() => fileInputRef.current?.click()}
            style={{
              padding: '4px 14px', borderRadius: 20, border: '2px dashed #FF9500',
              cursor: 'pointer', fontSize: '0.82rem', color: '#FF9500', fontWeight: 600, whiteSpace: 'nowrap',
            }}
          >
            {uploading ? uploadProgress || 'Uploading...' : '↑ Drop / Upload'}
          </div>
        )}
      </div>

      {/* Grid */}
      {loading ? (
        <div style={{ padding: '3rem', textAlign: 'center', color: '#6c757d' }}>Loading...</div>
      ) : photos.length === 0 ? (
        <div style={{ padding: '3rem', textAlign: 'center', color: '#aaa' }}>
          <div style={{ fontSize: '3rem', marginBottom: '0.5rem' }}>📷</div>
          <div>No photos yet — upload some to get started</div>
          {!jobId && <div style={{ marginTop: '0.5rem', fontSize: '0.85rem' }}>Use ⚙ Settings to configure where photos are stored on your server</div>}
        </div>
      ) : (
        <>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))', gap: '0.75rem', marginBottom: '1rem' }}>
            {photos.map(photo => {
              const cat = CAT_META[photo.category] || CAT_META.general;
              return (
                <div key={photo.id} style={{ position: 'relative', borderRadius: 8, overflow: 'hidden', background: '#f0f0f0', boxShadow: '0 1px 4px rgba(0,0,0,0.1)', cursor: 'pointer' }}>
                  <img
                    src={photo.url} alt={photo.caption || photo.originalFileName}
                    onClick={() => setLightbox(photo)}
                    style={{ width: '100%', aspectRatio: '4/3', objectFit: 'cover', display: 'block' }}
                    loading="lazy"
                    onError={e => { e.target.style.display = 'none'; }}
                  />
                  <div style={{ position: 'absolute', top: 6, left: 6 }}>
                    <span style={{ padding: '2px 7px', borderRadius: 10, fontSize: '0.68rem', fontWeight: 700, background: cat.bg, color: cat.color }}>{cat.label}</span>
                  </div>
                  {photo.storageBackend === 'ipfs' && (
                    <div style={{ position: 'absolute', top: 6, right: 6 }}>
                      <span style={{ padding: '2px 7px', borderRadius: 10, fontSize: '0.65rem', fontWeight: 700, background: '#f0ebff', color: '#6f42c1' }}>IPFS</span>
                    </div>
                  )}
                  <div style={{ padding: '0.4rem 0.5rem', fontSize: '0.75rem', color: '#495057', background: 'rgba(255,255,255,0.95)' }}>
                    <div style={{ fontWeight: 600, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{photo.caption || photo.originalFileName}</div>
                    <div style={{ color: '#aaa', fontSize: '0.7rem' }}>{fmtDate(photo.takenAt)} · {fmtBytes(photo.fileSizeBytes)}</div>
                  </div>
                </div>
              );
            })}
          </div>

          {pages > 1 && (
            <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'center', alignItems: 'center', marginTop: '1rem' }}>
              <button className="btn btn-sm" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>←</button>
              <span style={{ fontSize: '0.85rem', color: '#6c757d' }}>Page {page} of {pages} ({total} photos)</span>
              <button className="btn btn-sm" onClick={() => setPage(p => Math.min(pages, p + 1))} disabled={page === pages}>→</button>
            </div>
          )}
        </>
      )}

      {/* Lightbox */}
      {lightbox && (
        <div style={overlay} onClick={() => setLightbox(null)}>
          <div style={{ ...lbBox }} onClick={e => e.stopPropagation()}>
            <button onClick={() => { const i = photos.findIndex(p => p.id === lightbox.id); if (i > 0) setLightbox(photos[i - 1]); }}
              disabled={photos.findIndex(p => p.id === lightbox.id) === 0} style={navBtn}>‹</button>

            <img src={lightbox.url} alt={lightbox.caption || ''} style={{ maxWidth: '70vw', maxHeight: '80vh', objectFit: 'contain', borderRadius: 4 }} />

            <button onClick={() => { const i = photos.findIndex(p => p.id === lightbox.id); if (i < photos.length - 1) setLightbox(photos[i + 1]); }}
              disabled={photos.findIndex(p => p.id === lightbox.id) === photos.length - 1} style={navBtn}>›</button>

            <div style={{ position: 'absolute', bottom: 0, left: 0, right: 0, background: 'rgba(0,0,0,0.8)', color: '#fff', padding: '1rem', borderRadius: '0 0 8px 8px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '1rem' }}>
                <div style={{ flex: 1, minWidth: 0 }}>
                  {editingId === lightbox.id ? (
                    <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                      <select value={editForm.category} onChange={e => setEditForm(f => ({ ...f, category: e.target.value }))}
                        style={{ fontSize: '0.8rem', padding: '2px 6px', borderRadius: 4, border: 'none' }}>
                        {CATEGORIES.filter(c => c !== 'all').map(c => <option key={c} value={c}>{c}</option>)}
                      </select>
                      <input value={editForm.caption} onChange={e => setEditForm(f => ({ ...f, caption: e.target.value }))}
                        placeholder="Caption" style={{ flex: 1, fontSize: '0.8rem', padding: '2px 6px', borderRadius: 4, border: 'none', minWidth: 120 }} />
                      <input value={editForm.tags} onChange={e => setEditForm(f => ({ ...f, tags: e.target.value }))}
                        placeholder="Tags (comma-separated)" style={{ flex: 1, fontSize: '0.8rem', padding: '2px 6px', borderRadius: 4, border: 'none', minWidth: 120 }} />
                      <button onClick={() => saveEdit(lightbox.id)} style={lbBtn}>Save</button>
                      <button onClick={() => setEditingId(null)} style={{ ...lbBtn, background: '#555' }}>Cancel</button>
                    </div>
                  ) : (
                    <>
                      <div style={{ fontWeight: 700, fontSize: '0.95rem', marginBottom: '0.2rem' }}>{lightbox.caption || lightbox.originalFileName}</div>
                      <div style={{ fontSize: '0.78rem', color: '#aaa' }}>
                        {fmtDateTime(lightbox.takenAt)} · {fmtBytes(lightbox.fileSizeBytes)} · by {lightbox.uploadedBy}
                        {lightbox.jobNumber && <> · Job <span style={{ color: '#FF9500' }}>{lightbox.jobNumber}</span></>}
                      </div>
                      {lightbox.tags && <div style={{ fontSize: '0.75rem', color: '#bbb', marginTop: '0.2rem' }}>{lightbox.tags}</div>}
                      {lightbox.storageBackend === 'ipfs' && lightbox.ipfsCid && (
                        <div style={{ fontSize: '0.72rem', color: '#a78bfa', marginTop: '0.2rem' }}>
                          IPFS: <a href={lightbox.url} target="_blank" rel="noopener noreferrer" style={{ color: '#a78bfa' }}>{lightbox.ipfsCid.slice(0, 16)}…</a>
                        </div>
                      )}
                    </>
                  )}
                </div>
                <div style={{ display: 'flex', gap: '0.4rem', flexShrink: 0 }}>
                  <a href={lightbox.url} download={lightbox.originalFileName} style={lbBtn}>↓</a>
                  <button onClick={() => startEdit(lightbox)} style={lbBtn}>Edit</button>
                  <button onClick={() => handleDelete(lightbox.id)} style={{ ...lbBtn, background: '#dc3545' }}>Delete</button>
                  <button onClick={() => setLightbox(null)} style={{ ...lbBtn, background: '#555' }}>✕</button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function SCard({ label, value, color, text }) {
  return (
    <div className="card" style={{ padding: '1rem 1.25rem' }}>
      <div style={{ fontSize: '0.75rem', fontWeight: 700, color: '#6c757d', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '0.4rem' }}>{label}</div>
      <div style={{ fontSize: text ? '1.2rem' : '2rem', fontWeight: 800, color }}>{value ?? 0}</div>
    </div>
  );
}

const overlay = { position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.9)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 2000 };
const lbBox   = { position: 'relative', display: 'flex', alignItems: 'center', gap: '0.5rem', maxWidth: '95vw' };
const navBtn  = { background: 'rgba(255,255,255,0.15)', border: 'none', color: '#fff', fontSize: '2.5rem', padding: '0.5rem 0.75rem', borderRadius: 8, cursor: 'pointer', lineHeight: 1, flexShrink: 0 };
const lbBtn   = { background: '#FF9500', border: 'none', color: '#fff', padding: '4px 10px', borderRadius: 6, cursor: 'pointer', fontSize: '0.8rem', textDecoration: 'none', display: 'inline-block' };
