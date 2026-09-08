function App() {
  return (
    <div className="app-shell">
      <header className="topbar">
        <a className="brand" href="/" aria-label="MilanSetu home">
          <span className="brand-mark">♥</span>
          <span>MilanSetu</span>
        </a>
        <nav className="nav-links" aria-label="Main navigation">
          <a href="#how-it-works">How it works</a>
          <a href="#safety">Safety</a>
          <a href="#about">About</a>
          <button className="login-button">Log in</button>
          <button className="primary-button small">Create profile</button>
        </nav>
      </header>

      <main>
        <section className="hero">
          <div className="hero-copy">
            <p className="eyebrow">FREE • PRIVATE • MEANINGFUL</p>
            <h1>Find someone to build a life with.</h1>
            <p className="hero-text">
              MilanSetu is a free, privacy-first matrimonial platform built around
              compatibility, trust and meaningful connections.
            </p>
            <div className="hero-actions">
              <button className="primary-button">Create your free profile</button>
              <button className="secondary-button">Explore how it works</button>
            </div>
            <p className="no-paywall">No subscription • No premium profile • No paid messaging</p>
          </div>

          <div className="hero-card" aria-label="MilanSetu values">
            <div className="heart-orbit">♥</div>
            <h2>A better way to meet.</h2>
            <div className="value-row"><span>✓</span><div><strong>Compatibility first</strong><small>Preferences you control.</small></div></div>
            <div className="value-row"><span>✓</span><div><strong>Privacy by design</strong><small>Your contact details stay private.</small></div></div>
            <div className="value-row"><span>✓</span><div><strong>Safety built in</strong><small>Report, block and moderation tools.</small></div></div>
          </div>
        </section>

        <section className="principles" id="how-it-works">
          <div><span>01</span><h3>Tell us about you</h3><p>Build a profile around your values, family, lifestyle and marriage expectations.</p></div>
          <div><span>02</span><h3>Set your preferences</h3><p>Choose what matters to you — from city and language to community and lifestyle.</p></div>
          <div><span>03</span><h3>Connect by mutual choice</h3><p>Express interest, accept, and start a conversation without exposing your phone number.</p></div>
        </section>

        <section className="safety-banner" id="safety">
          <div><p className="eyebrow">SAFETY FIRST</p><h2>Respectful connections, protected users.</h2></div>
          <p>Abuse detection, reporting, blocking, scam signals and human moderation will be part of the platform from the beginning.</p>
        </section>
      </main>

      <footer id="about"><span>♥ MilanSetu</span><span>Built for meaningful connections.</span></footer>
    </div>
  )
}

export default App
