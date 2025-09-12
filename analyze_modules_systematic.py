#!/usr/bin/env python3
"""
Systematic Module Analysis Script
Analyzes all modules and their routes to identify consolidation opportunities
"""

import json
import subprocess
import sys
from typing import Dict, List, Any, Set
from dataclasses import dataclass
from collections import defaultdict

@dataclass
class RouteInfo:
    path: str
    method: str
    module: str
    description: str = ""

@dataclass
class ModuleInfo:
    id: str
    name: str
    version: str
    routes: List[RouteInfo] = None
    main_topic: str = ""
    consolidation_target: str = ""

def get_all_routes() -> List[Dict[str, Any]]:
    """Get all API routes from the system"""
    try:
        # Get all nodes with type codex.meta/api
        result = subprocess.run([
            'curl', '-s', 'http://localhost:5000/nodes?typeId=codex.meta/api'
        ], capture_output=True, text=True, check=True)
        
        nodes = json.loads(result.stdout)
        api_nodes = [node for node in nodes if 'api/' in node.get('id', '')]
        
        routes = []
        for node in api_nodes:
            if 'path' in node.get('meta', {}):
                routes.append({
                    'id': node['id'],
                    'path': node['meta'].get('path', ''),
                    'method': node['meta'].get('method', 'GET'),
                    'description': node.get('title', ''),
                    'module': node['meta'].get('module', 'unknown')
                })
        
        return routes
    except Exception as e:
        print(f"Error getting routes: {e}")
        return []

def get_loaded_modules() -> List[ModuleInfo]:
    """Get all loaded modules"""
    try:
        result = subprocess.run([
            'curl', '-s', 'http://localhost:5000/modules/loading-report'
        ], capture_output=True, text=True, check=True)
        
        data = json.loads(result.stdout)
        modules = []
        
        for module_data in data['modules']:
            modules.append(ModuleInfo(
                id=module_data['id'],
                name=module_data['name'],
                version=module_data['version']
            ))
        
        return modules
    except Exception as e:
        print(f"Error getting modules: {e}")
        return []

def analyze_module_topics(modules: List[ModuleInfo]) -> Dict[str, str]:
    """Analyze module topics based on names and IDs"""
    topic_mapping = {}
    
    for module in modules:
        module_id = module.id.lower()
        module_name = module.name.lower()
        
        # AI and LLM related
        if any(keyword in module_id or keyword in module_name for keyword in 
               ['ai', 'llm', 'llm.future', 'llm.response', 'ucore.llm']):
            topic_mapping[module.id] = 'AI/LLM'
        
        # Translation related
        elif any(keyword in module_id or keyword in module_name for keyword in 
                 ['translation', 'translate', 'language']):
            topic_mapping[module.id] = 'Translation'
        
        # Concept related
        elif any(keyword in module_id or keyword in module_name for keyword in 
                 ['concept', 'concept-registry', 'userconcept']):
            topic_mapping[module.id] = 'Concept'
        
        # Joy and Resonance related
        elif any(keyword in module_id or keyword in module_name for keyword in 
                 ['joy', 'resonance', 'ucore.joy', 'resonance-joy']):
            topic_mapping[module.id] = 'Joy/Resonance'
        
        # Storage related
        elif any(keyword in module_id or keyword in module_name for keyword in 
                 ['storage', 'distributed-storage', 'storage-endpoints']):
            topic_mapping[module.id] = 'Storage'
        
        # Security related
        elif any(keyword in module_id or keyword in module_name for keyword in 
                 ['security', 'auth', 'access-control', 'digital-signature', 'identity']):
            topic_mapping[module.id] = 'Security'
        
        # Core system
        elif any(keyword in module_id or keyword in module_name for keyword in 
                 ['core', 'breath', 'composer', 'delta', 'phase', 'plan', 'relations']):
            topic_mapping[module.id] = 'Core System'
        
        # User related
        elif any(keyword in module_id or keyword in module_name for keyword in 
                 ['user', 'user-contributions']):
            topic_mapping[module.id] = 'User Management'
        
        # Communication
        elif any(keyword in module_id or keyword in module_name for keyword in 
                 ['realtime', 'event-streaming', 'push-notifications']):
            topic_mapping[module.id] = 'Communication'
        
        # Analysis and Intelligence
        elif any(keyword in module_id or keyword in module_name for keyword in 
                 ['analysis', 'intelligent', 'caching', 'load-balancing']):
            topic_mapping[module.id] = 'Analysis/Intelligence'
        
        # API and Documentation
        elif any(keyword in module_id or keyword in module_name for keyword in 
                 ['openapi', 'spec', 'reflect', 'oneshot']):
            topic_mapping[module.id] = 'API/Documentation'
        
        # Future and Knowledge
        elif any(keyword in module_id or keyword in module_name for keyword in 
                 ['future', 'knowledge']):
            topic_mapping[module.id] = 'Future/Knowledge'
        
        else:
            topic_mapping[module.id] = 'Other'
    
    return topic_mapping

def suggest_consolidation_targets(modules: List[ModuleInfo], topic_mapping: Dict[str, str]) -> Dict[str, str]:
    """Suggest consolidation targets for modules"""
    consolidation_targets = {}
    
    # Define target modules for consolidation
    target_modules = {
        'AI/LLM': 'AIModule',
        'Translation': 'TranslationModule', 
        'Concept': 'ConceptModule',
        'Joy/Resonance': 'JoyModule',
        'Storage': 'StorageModule',
        'Security': 'SecurityModule',
        'User Management': 'UserModule',
        'Communication': 'CommunicationModule',
        'Analysis/Intelligence': 'IntelligenceModule',
        'API/Documentation': 'APIModule',
        'Future/Knowledge': 'KnowledgeModule'
    }
    
    for module in modules:
        topic = topic_mapping.get(module.id, 'Other')
        if topic in target_modules:
            consolidation_targets[module.id] = target_modules[topic]
        else:
            consolidation_targets[module.id] = 'Keep Separate'
    
    return consolidation_targets

def main():
    print("🔍 Starting Systematic Module Analysis...")
    
    # Get all modules and routes
    print("📋 Getting loaded modules...")
    modules = get_loaded_modules()
    print(f"Found {len(modules)} modules")
    
    print("🛣️ Getting API routes...")
    routes = get_all_routes()
    print(f"Found {len(routes)} API routes")
    
    # Analyze module topics
    print("🎯 Analyzing module topics...")
    topic_mapping = analyze_module_topics(modules)
    
    # Suggest consolidation targets
    print("🔄 Suggesting consolidation targets...")
    consolidation_targets = suggest_consolidation_targets(modules, topic_mapping)
    
    # Group modules by topic
    topic_groups = defaultdict(list)
    for module in modules:
        topic = topic_mapping.get(module.id, 'Other')
        topic_groups[topic].append(module)
    
    # Print analysis results
    print("\n" + "="*80)
    print("📊 MODULE CONSOLIDATION ANALYSIS")
    print("="*80)
    
    for topic, topic_modules in topic_groups.items():
        print(f"\n🎯 {topic} ({len(topic_modules)} modules)")
        print("-" * 50)
        
        for module in topic_modules:
            target = consolidation_targets.get(module.id, 'Keep Separate')
            status = "✅" if target != 'Keep Separate' else "🔸"
            print(f"  {status} {module.name} ({module.id})")
            print(f"      → Consolidate into: {target}")
    
    # Identify high-priority consolidations
    print("\n" + "="*80)
    print("🚀 HIGH-PRIORITY CONSOLIDATION TARGETS")
    print("="*80)
    
    priority_consolidations = {
        'AI/LLM': ['ai-module', 'codex.llm.future', 'codex.llm.response-handler'],
        'Translation': [],  # TranslationModule was removed in git reset
        'Concept': ['codex.concept', 'codex.concept-registry', 'codex.userconcept'],
        'Joy/Resonance': ['codex.joy.calculator', 'codex.resonance-joy', 'codex.ucore.joy', 'ucore-resonance-engine'],
        'Storage': ['codex.storage', 'codex.distributed-storage', 'codex.storage-endpoints']
    }
    
    for topic, module_ids in priority_consolidations.items():
        if module_ids:
            print(f"\n🎯 {topic}")
            for module_id in module_ids:
                module = next((m for m in modules if m.id == module_id), None)
                if module:
                    print(f"  • {module.name} ({module.id})")
    
    # Save detailed analysis
    analysis_data = {
        'modules': [
            {
                'id': module.id,
                'name': module.name,
                'version': module.version,
                'topic': topic_mapping.get(module.id, 'Other'),
                'consolidation_target': consolidation_targets.get(module.id, 'Keep Separate')
            }
            for module in modules
        ],
        'routes': routes,
        'topic_groups': {topic: [m.id for m in modules] for topic, modules in topic_groups.items()},
        'priority_consolidations': priority_consolidations
    }
    
    with open('systematic_module_analysis.json', 'w') as f:
        json.dump(analysis_data, f, indent=2)
    
    print(f"\n💾 Detailed analysis saved to: systematic_module_analysis.json")
    print(f"📈 Total modules: {len(modules)}")
    print(f"🛣️ Total routes: {len(routes)}")
    print(f"🎯 Topics identified: {len(topic_groups)}")

if __name__ == "__main__":
    main()
