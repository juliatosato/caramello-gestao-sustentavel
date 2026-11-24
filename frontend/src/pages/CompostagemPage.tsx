import { ArrowLeft, Check, CircleAlert, Droplets, Layers3, RefreshCw, X } from 'lucide-react';

export default function CompostagemPage({ onVoltar }: { onVoltar: () => void }) {
  return <>
    <button type="button" className="backButton" onClick={onVoltar}><ArrowLeft size={17} /> Voltar para borra de café</button>

    <section className="panel compostIntro">
      <div><span className="compostEyebrow">COMECE EM PEQUENA ESCALA</span><h2>Antes de montar a composteira</h2><p>Escolha um local ventilado, protegido de chuva e sol forte, com piso lavável e distante da área de preparo de alimentos. Separe recipientes identificados para impedir a mistura com plástico, metal, vidro ou produtos de limpeza.</p></div>
      <div className="destinationNotice" role="note"><strong>Atenção ao volume da cafeteria</strong><span>Se a geração diária for maior que a capacidade de manejo, priorize uma coleta especializada. O objetivo é tratar corretamente os resíduos, sem criar odor, líquido acumulado ou atração de animais.</span></div>
    </section>

    <section className="compostMaterials" aria-label="Materiais para compostagem">
      <article className="compostMaterialCard allowed"><div className="materialTitle"><Check size={20} /><h2>Pode colocar</h2></div><ul><li>Borra e filtro de café de papel</li><li>Cascas e talos de frutas, legumes e verduras</li><li>Cascas de ovos trituradas</li><li>Folhas e sachês de chá sem plástico ou grampo</li><li>Folhas secas e pequenas podas sem produtos químicos</li><li>Papelão sem plastificação, picado em pouca quantidade</li></ul></article>
      <article className="compostMaterialCard avoid"><div className="materialTitle"><X size={20} /><h2>Não deve colocar</h2></div><ul><li>Carnes, peixes, ossos e laticínios</li><li>Óleo, gordura, molhos ou alimentos muito temperados</li><li>Plástico, vidro, metal e embalagens</li><li>Produtos de limpeza ou outros produtos químicos</li><li>Fezes de animais e resíduos sanitários</li><li>Bitucas de cigarro, cinzas ou madeira tratada</li></ul></article>
    </section>

    <section className="panel"><h2>Passo a passo básico</h2><div className="compostSteps">
      <article><span>1</span><Layers3 /><div><strong>Prepare uma base seca</strong><p>Comece com folhas secas, serragem de madeira não tratada ou papelão picado.</p></div></article>
      <article><span>2</span><Droplets /><div><strong>Adicione os resíduos úmidos</strong><p>Pique cascas e talos. Distribua a borra sem formar uma camada grossa e compactada.</p></div></article>
      <article><span>3</span><Layers3 /><div><strong>Cubra novamente</strong><p>Cubra cada porção úmida com material seco. A camada superior deve permanecer seca.</p></div></article>
      <article><span>4</span><RefreshCw /><div><strong>Areje e acompanhe</strong><p>Revolva periodicamente. A mistura deve ficar úmida como uma esponja torcida, nunca encharcada.</p></div></article>
    </div></section>

    <section className="panel"><h2>Como corrigir problemas</h2><div className="compostTroubles">
      <article><CircleAlert /><div><strong>Mau cheiro ou excesso de umidade</strong><p>Adicione material seco, misture para entrar ar e reduza temporariamente os resíduos úmidos.</p></div></article>
      <article><CircleAlert /><div><strong>Muito seca e sem decomposição</strong><p>Acrescente pequenas porções de material úmido e misture. Não despeje grandes quantidades de água.</p></div></article>
      <article><CircleAlert /><div><strong>Moscas ou outros animais</strong><p>Confira se há alimentos inadequados, cubra totalmente os resíduos frescos e mantenha o recipiente fechado e ventilado.</p></div></article>
    </div></section>

    <section className="panel education"><h2>Quando o composto está pronto?</h2><p>O material deve estar escuro, solto, com cheiro de terra e sem restos reconhecíveis. Não use composto ainda em decomposição nas plantas. O tempo varia conforme volume, umidade, temperatura, tamanho dos resíduos e frequência de aeração.</p><p className="sourceNote">Orientação educativa baseada em materiais da Embrapa e de órgãos públicos ambientais. Para implantar uma operação contínua na cafeteria, procure orientação técnica e confira as regras sanitárias e ambientais locais.</p></section>
  </>;
}
