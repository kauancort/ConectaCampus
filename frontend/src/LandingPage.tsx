export default function LandingPage({ onRegister, onLogin }: { onRegister: () => void; onLogin: () => void }) {
  return <div className="landing-page">
    <section className="catalog-intro landing-hero" aria-labelledby="landing-title">
      <div className="intro-copy">
        
        <h1 id="landing-title">Todo mundo tem algo <em>para ensinar.</em></h1>
        <p>O ConectaCampus aproxima alunos que querem compartilhar o que sabem de pessoas que querem aprender — com o acompanhamento de professores.</p>
        <div className="landing-actions"><button className="button primary" type="button" onClick={onRegister}>Criar minha conta <span aria-hidden="true">↗</span></button><a href="#como-funciona" className="landing-secondary">Conheça a plataforma <span aria-hidden="true">↓</span></a></div>
        
      </div>
      <div className="intro-art" aria-hidden="true"><div className="art-orbit orbit-one" /><div className="art-orbit orbit-two" /><div className="art-paper paper-back" /><div className="art-paper paper-middle" /><div className="art-paper paper-front"><span>conhecimento<br />se constrói<br /><strong>junto.</strong></span><i>✳</i></div></div>
    </section>
    <section className="landing-intro" id="como-funciona"><div><h2>Aprender, compartilhar<br /><em>e crescer junto.</em></h2></div><div><p>No campus, o conhecimento também circula entre colegas. A plataforma organiza esses encontros e dá espaço para que alunos aprendam uns com os outros.</p><p>Professores acompanham as propostas para que cada aula aconteça com responsabilidade.</p></div></section>
    <section className="landing-steps"><div className="landing-section-heading"><div><h2>Como funciona<span>.</span></h2></div><p>Um caminho para participar ou compartilhar o que você sabe.</p></div><div className="landing-steps-grid"><article><h3>Encontre uma aula.</h3><p>Depois de entrar, explore as atividades disponíveis e escolha um assento na sala.</p><b aria-hidden="true">↗</b></article><article><h3>Proponha seu encontro.</h3><p>Solicite autorização para ministrar e envie sua ideia de aula para avaliação.</p><b aria-hidden="true">✳</b></article><article><h3>Aprenda com apoio.</h3><p>Um professor avalia a proposta e acompanha as aulas que estiverem sob sua responsabilidade.</p><b aria-hidden="true">↗</b></article></div></section>
    <section className="landing-invitation" aria-label="Acesse o ConectaCampus">
      <div className="landing-actions">
        <button className="button primary" type="button" onClick={onRegister}>Criar conta</button>
        <button className="button secondary" type="button" onClick={onLogin}>Já tenho conta</button>
      </div>
    </section>
  </div>
}
