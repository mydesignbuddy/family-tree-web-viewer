import {
  Component, Input, Output, EventEmitter,
  ElementRef, OnChanges, OnDestroy, SimpleChanges, AfterViewInit
} from '@angular/core';
import * as d3 from 'd3';
import { FamilyTreeData, Individual } from '../../models/family-tree.model';
import { TreeDataService } from '../../services/tree-data.service';

interface FanEntry {
  person: Individual;
  generation: number;
  position: number;
  startAngle: number;
  endAngle: number;
  innerRadius: number;
  outerRadius: number;
  isPaternal: boolean;
}

@Component({
  selector: 'app-fan-chart',
  standalone: true,
  template: '<div class="fan-container"></div>',
  styles: [`
    :host { display: block; width: 100%; height: 100%; }
    .fan-container { width: 100%; height: 100%; }
  `]
})
export class FanChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input() data!: FamilyTreeData;
  @Input() rootPersonId!: string;
  @Output() personClicked = new EventEmitter<Individual>();

  private svg: any;
  private zoomGroup: any;
  private resizeObserver?: ResizeObserver;

  private readonly MAX_GENERATIONS = 6;
  private readonly RING_WIDTH = 52;
  private readonly CENTER_RADIUS = 50;
  private readonly FAN_SPAN = Math.PI; // semi-circle (180 degrees)

  // Color palettes for paternal (blue) and maternal (green) sides
  private readonly PATERNAL_COLORS = ['#4a90d9', '#6aa3e0', '#8ab7e8', '#a8caf0', '#c5ddf7', '#dceafb'];
  private readonly MATERNAL_COLORS = ['#2a7d3f', '#4a9a5c', '#6db47a', '#93cc9b', '#b8e0bd', '#d8f0db'];

  constructor(
    private el: ElementRef,
    private treeDataService: TreeDataService
  ) {}

  ngAfterViewInit(): void {
    this.initSvg();
    this.render();
    this.resizeObserver = new ResizeObserver(() => this.render());
    this.resizeObserver.observe(this.el.nativeElement);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.svg && (changes['rootPersonId'] || changes['data'])) {
      this.render();
    }
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }

  private initSvg(): void {
    const container = this.el.nativeElement.querySelector('.fan-container');
    this.svg = d3.select(container)
      .append('svg')
      .attr('width', '100%')
      .attr('height', '100%');

    this.zoomGroup = this.svg.append('g');

    const zoom = d3.zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.3, 3])
      .on('zoom', (event: d3.D3ZoomEvent<SVGSVGElement, unknown>) => {
        this.zoomGroup.attr('transform', event.transform);
      });

    this.svg.call(zoom);
  }

  private render(): void {
    if (!this.data || !this.rootPersonId) return;

    this.zoomGroup.selectAll('*').remove();

    const container = this.el.nativeElement.querySelector('.fan-container');
    const width = container.clientWidth;
    const height = container.clientHeight;
    const centerX = width / 2;
    const centerY = height * 0.75;

    const chartGroup = this.zoomGroup.append('g')
      .attr('transform', `translate(${centerX},${centerY})`);

    // Build ancestor data
    const ancestorTree = this.treeDataService.buildAncestorTree(this.rootPersonId, this.data, 0, this.MAX_GENERATIONS);
    if (!ancestorTree) return;

    // Flatten to fan entries
    const entries = this.buildFanEntries(ancestorTree);

    // Draw arc segments
    const arcGenerator = d3.arc<FanEntry>()
      .innerRadius(d => d.innerRadius)
      .outerRadius(d => d.outerRadius)
      .startAngle(d => d.startAngle)
      .endAngle(d => d.endAngle)
      .padAngle(0.01)
      .cornerRadius(3);

    // Draw arcs
    const arcs = chartGroup.selectAll('.arc')
      .data(entries)
      .join('g')
      .attr('class', 'arc')
      .style('cursor', 'pointer')
      .on('click', (_event: MouseEvent, d: FanEntry) => {
        this.personClicked.emit(d.person);
      });

    arcs.append('path')
      .attr('d', arcGenerator as any)
      .attr('fill', (d: FanEntry) => {
        const colors = d.isPaternal ? this.PATERNAL_COLORS : this.MATERNAL_COLORS;
        return colors[Math.min(d.generation - 1, colors.length - 1)];
      })
      .attr('stroke', '#ffffff')
      .attr('stroke-width', 1.5);

    // Add text labels along arcs
    arcs.each((d: FanEntry, i: number, elements: SVGGElement[] | ArrayLike<SVGGElement>) => {
      const g = d3.select(elements[i]);
      const midAngle = (d.startAngle + d.endAngle) / 2;
      const midRadius = (d.innerRadius + d.outerRadius) / 2;

      // Calculate text position
      const x = midRadius * Math.cos(midAngle - Math.PI / 2);
      const y = midRadius * Math.sin(midAngle - Math.PI / 2);

      // Calculate rotation - keep text readable
      let rotation = (midAngle * 180 / Math.PI) - 90;
      const flip = rotation > 90 && rotation < 270;
      if (flip) rotation -= 180;

      // Only show text if arc is wide enough
      const arcAngle = d.endAngle - d.startAngle;
      const arcLength = arcAngle * midRadius;

      if (arcLength > 30) {
        const name = this.treeDataService.getDisplayName(d.person);
        const lifespan = this.treeDataService.getLifespan(d.person);

        // Truncate name if needed
        const maxChars = Math.floor(arcLength / 7);
        const displayName = name.length > maxChars ? name.substring(0, maxChars - 2) + '...' : name;

        g.append('text')
          .attr('transform', `translate(${x},${y}) rotate(${rotation})`)
          .attr('text-anchor', 'middle')
          .attr('dominant-baseline', 'middle')
          .attr('font-size', d.generation <= 2 ? '11px' : d.generation <= 4 ? '9px' : '7px')
          .attr('font-weight', d.generation <= 2 ? '600' : '400')
          .attr('fill', d.generation <= 2 ? '#ffffff' : '#2c3e50')
          .attr('font-family', "'Inter', sans-serif")
          .attr('pointer-events', 'none')
          .text(displayName);

        // Show lifespan for inner generations
        if (d.generation <= 3 && arcLength > 60) {
          const lifespanY = d.generation <= 2 ? 13 : 10;
          g.append('text')
            .attr('transform', `translate(${x},${y}) rotate(${rotation})`)
            .attr('text-anchor', 'middle')
            .attr('dominant-baseline', 'middle')
            .attr('dy', lifespanY)
            .attr('font-size', d.generation <= 2 ? '9px' : '7px')
            .attr('fill', d.generation <= 2 ? 'rgba(255,255,255,0.8)' : '#7f8c8d')
            .attr('font-family', "'Inter', sans-serif")
            .attr('pointer-events', 'none')
            .text(lifespan);
        }
      }
    });

    // Hover effects
    arcs.on('mouseenter', function(this: SVGGElement) {
      d3.select(this).select('path')
        .transition().duration(200)
        .attr('opacity', 0.8)
        .attr('stroke-width', 2.5);
    }).on('mouseleave', function(this: SVGGElement) {
      d3.select(this).select('path')
        .transition().duration(200)
        .attr('opacity', 1)
        .attr('stroke-width', 1.5);
    });

    // Draw center circle for root person
    const rootPerson = this.data.individuals[this.rootPersonId];
    if (rootPerson) {
      const centerGroup = chartGroup.append('g')
        .style('cursor', 'pointer')
        .on('click', () => this.personClicked.emit(rootPerson));

      centerGroup.append('circle')
        .attr('r', this.CENTER_RADIUS)
        .attr('fill', '#2c3e50')
        .attr('stroke', '#fff')
        .attr('stroke-width', 3);

      centerGroup.append('text')
        .attr('text-anchor', 'middle')
        .attr('dy', '-0.3em')
        .attr('font-size', '12px')
        .attr('font-weight', '700')
        .attr('fill', '#ffffff')
        .attr('font-family', "'Inter', sans-serif")
        .text(this.treeDataService.getDisplayName(rootPerson));

      centerGroup.append('text')
        .attr('text-anchor', 'middle')
        .attr('dy', '1.1em')
        .attr('font-size', '10px')
        .attr('fill', 'rgba(255,255,255,0.7)')
        .attr('font-family', "'Inter', sans-serif")
        .text(this.treeDataService.getLifespan(rootPerson));
    }

    // Legend
    this.drawLegend(chartGroup, centerX);
  }

  private buildFanEntries(
    node: any,
    generation: number = 1,
    startAngle: number = -this.FAN_SPAN / 2,
    endAngle: number = this.FAN_SPAN / 2,
    isPaternal: boolean = true
  ): FanEntry[] {
    const entries: FanEntry[] = [];

    if (generation > this.MAX_GENERATIONS) return entries;

    const midAngle = (startAngle + endAngle) / 2;
    const innerRadius = this.CENTER_RADIUS + (generation - 1) * this.RING_WIDTH + 4;
    const outerRadius = this.CENTER_RADIUS + generation * this.RING_WIDTH;

    // Father (top half) and mother (bottom half) of each person
    if (node.father) {
      const fatherStart = startAngle;
      const fatherEnd = midAngle;
      entries.push({
        person: node.father.person,
        generation,
        position: 0,
        startAngle: fatherStart,
        endAngle: fatherEnd,
        innerRadius,
        outerRadius,
        isPaternal: generation === 1 ? true : isPaternal
      });
      entries.push(...this.buildFanEntries(node.father, generation + 1, fatherStart, fatherEnd, generation === 1 ? true : isPaternal));
    }

    if (node.mother) {
      const motherStart = midAngle;
      const motherEnd = endAngle;
      entries.push({
        person: node.mother.person,
        generation,
        position: 1,
        startAngle: motherStart,
        endAngle: motherEnd,
        innerRadius,
        outerRadius,
        isPaternal: generation === 1 ? false : isPaternal
      });
      entries.push(...this.buildFanEntries(node.mother, generation + 1, motherStart, motherEnd, generation === 1 ? false : isPaternal));
    }

    return entries;
  }

  private drawLegend(chartGroup: any, centerX: number): void {
    const legend = chartGroup.append('g')
      .attr('transform', `translate(${-centerX + 20}, ${-20})`);

    legend.append('rect').attr('width', 12).attr('height', 12).attr('rx', 2).attr('fill', this.PATERNAL_COLORS[0]);
    legend.append('text').attr('x', 18).attr('y', 10).attr('font-size', '11px').attr('fill', '#5a6c7d')
      .attr('font-family', "'Inter', sans-serif").text('Paternal line');

    legend.append('rect').attr('y', 20).attr('width', 12).attr('height', 12).attr('rx', 2).attr('fill', this.MATERNAL_COLORS[0]);
    legend.append('text').attr('x', 18).attr('y', 30).attr('font-size', '11px').attr('fill', '#5a6c7d')
      .attr('font-family', "'Inter', sans-serif").text('Maternal line');
  }
}
