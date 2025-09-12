#!/usr/bin/env python3
"""
Script to add spec references to all modules that don't have them yet.
This will help us track which modules belong to which specs.
"""

import requests
import json
import sys

BASE_URL = "http://localhost:5001"

def get_all_modules():
    """Get all modules from the system"""
    try:
        response = requests.get(f"{BASE_URL}/spec/modules/all")
        if response.status_code == 200:
            data = response.json()
            return data.get('modules', [])
        else:
            print(f"Error getting modules: {response.status_code}")
            return []
    except Exception as e:
        print(f"Error getting modules: {e}")
        return []

def get_module_details(module_id):
    """Get detailed module information"""
    try:
        response = requests.get(f"{BASE_URL}/nodes/{module_id}")
        if response.status_code == 200:
            return response.json()
        else:
            print(f"Error getting module {module_id}: {response.status_code}")
            return None
    except Exception as e:
        print(f"Error getting module {module_id}: {e}")
        return None

def update_module_spec_reference(module_id, spec_reference):
    """Update a module with a spec reference"""
    try:
        # Get current module data
        module_data = get_module_details(module_id)
        if not module_data:
            return False
        
        # Add spec reference to meta
        if 'meta' not in module_data:
            module_data['meta'] = {}
        
        module_data['meta']['specReference'] = spec_reference
        
        # Update the module
        response = requests.put(f"{BASE_URL}/nodes/{module_id}", json=module_data)
        if response.status_code == 200:
            print(f"✓ Updated {module_id} with spec reference: {spec_reference}")
            return True
        else:
            print(f"✗ Failed to update {module_id}: {response.status_code}")
            return False
    except Exception as e:
        print(f"✗ Error updating {module_id}: {e}")
        return False

def main():
    print("Adding spec references to modules...")
    
    # Get all modules
    modules = get_all_modules()
    if not modules:
        print("No modules found")
        return
    
    print(f"Found {len(modules)} modules")
    
    # Define spec references for modules that don't have them
    spec_mappings = {
        "codex.hello": "codex.spec.hello",
        "codex.core": "codex.spec.core",
        "codex.storage": "codex.spec.storage",
        "codex.authentication": "codex.spec.authentication",
        "codex.user": "codex.spec.user",
        "codex.breath": "codex.spec.breath",
        "codex.delta": "codex.spec.delta",
        "codex.hydrate": "codex.spec.hydrate",
        "codex.adapter": "codex.spec.adapter",
        "codex.concept": "codex.spec.concept",
        "codex.ai": "codex.spec.ai",
        "codex.llm-future-knowledge": "codex.spec.llm-future-knowledge",
        "codex.self-update": "codex.spec.self-update",
        "codex.ucore-llm-response-handler": "codex.spec.ucore-llm-response-handler",
        "codex.system-metrics": "codex.spec.system-metrics",
        "codex.service-discovery": "codex.spec.service-discovery",
        "codex.graph-query": "codex.spec.graph-query",
        "codex.user-contributions": "codex.spec.user-contributions",
        "codex.llm-response-handler": "codex.spec.llm-response-handler",
        "codex.intelligent-caching": "codex.spec.intelligent-caching",
        "codex.concept-image": "codex.spec.concept-image",
        "codex.realtime-news-stream": "codex.spec.realtime-news-stream",
        "codex.load-balancing": "codex.spec.load-balancing",
        "codex.security": "codex.spec.security",
        "codex.event-streaming": "codex.spec.event-streaming",
        "codex.realtime": "codex.spec.realtime",
        "codex.push-notification": "codex.spec.push-notification",
        "codex.spec": "codex.spec.spec",
        "codex.spec-driven": "codex.spec.spec-driven",
        "codex.future-knowledge": "codex.spec.future-knowledge",
        "codex.test-dynamic": "codex.spec.test-dynamic",
        "codex.dynamic-module-example": "codex.spec.dynamic-module-example",
        "codex.endpoint-generation-demo": "codex.spec.endpoint-generation-demo",
        "codex.ucore-resonance-engine": "codex.spec.ucore-resonance-engine",
        "codex.news-feed": "codex.spec.news-feed",
        "codex.image-analysis": "codex.spec.image-analysis",
        "codex.translation": "codex.spec.translation",
        "codex.joy-calculator": "codex.spec.joy-calculator",
        "codex.ucore-joy": "codex.spec.ucore-joy",
        "codex.resonance-joy": "codex.spec.resonance-joy",
        "codex.comprehensive-ai": "codex.spec.comprehensive-ai",
        "codex.ai-analysis": "codex.spec.ai-analysis",
        "codex.realtime-news-stream-simple": "codex.spec.realtime-news-stream-simple"
    }
    
    updated_count = 0
    
    for module in modules:
        module_id = module.get('id', '')
        if not module_id:
            continue
        
        # Check if module already has a spec reference
        module_details = get_module_details(module_id)
        if module_details and module_details.get('meta', {}).get('specReference'):
            print(f"- {module_id} already has spec reference")
            continue
        
        # Get spec reference for this module
        spec_reference = spec_mappings.get(module_id)
        if not spec_reference:
            print(f"- No spec mapping for {module_id}")
            continue
        
        # Update the module
        if update_module_spec_reference(module_id, spec_reference):
            updated_count += 1
    
    print(f"\nUpdated {updated_count} modules with spec references")
    
    # Show final status
    print("\nFinal spec-to-module relationships:")
    try:
        response = requests.get(f"{BASE_URL}/spec/relationships/spec-to-modules")
        if response.status_code == 200:
            data = response.json()
            print(f"Total spec-to-module edges: {data.get('totalSpecToModule', 0)}")
            print(f"Total module-to-spec edges: {data.get('totalModuleToSpec', 0)}")
        else:
            print(f"Error getting relationships: {response.status_code}")
    except Exception as e:
        print(f"Error getting relationships: {e}")

if __name__ == "__main__":
    main()
