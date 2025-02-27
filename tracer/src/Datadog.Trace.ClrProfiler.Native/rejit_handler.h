#ifndef DD_CLR_PROFILER_REJIT_HANDLER_H_
#define DD_CLR_PROFILER_REJIT_HANDLER_H_

#include <atomic>
#include <mutex>
#include <shared_mutex>
#include <string>
#include <unordered_map>
#include <vector>
#include <future>

#include "cor.h"
#include "corprof.h"
#include "module_metadata.h"

namespace trace
{

typedef std::shared_mutex Lock;
typedef std::unique_lock<Lock> WriteLock;
typedef std::shared_lock<Lock> ReadLock;

struct RejitItem
{
    int m_type = 0;
    std::unique_ptr<std::vector<ModuleID>> m_modulesId = nullptr;
    std::unique_ptr<std::vector<mdMethodDef>> m_methodDefs = nullptr;
    std::unique_ptr<std::vector<IntegrationMethod>> m_integrationMethods = nullptr;
    //
    std::promise<ULONG>* m_promise = nullptr;

    RejitItem();

    RejitItem(std::unique_ptr<std::vector<ModuleID>>&& modulesId,
              std::unique_ptr<std::vector<mdMethodDef>>&& methodDefs);

    RejitItem(std::unique_ptr<std::vector<ModuleID>>&& modulesId,
              std::unique_ptr<std::vector<IntegrationMethod>>&& integrationMethods, std::promise<ULONG>* promise);

    static std::unique_ptr<RejitItem> CreateEndRejitThread();
};

// forward declarations...
class RejitHandlerModule;
class RejitHandler;

/// <summary>
/// Rejit handler representation of a method
/// </summary>
class RejitHandlerModuleMethod
{
private:
    mdMethodDef m_methodDef;
    ICorProfilerFunctionControl* m_pFunctionControl;
    std::unique_ptr<FunctionInfo> m_functionInfo;
    std::unique_ptr<MethodReplacement> m_methodReplacement;

    std::mutex m_ngenModulesLock;
    std::unordered_map<ModuleID, bool> m_ngenModules;

    RejitHandlerModule* m_module;

public:
    RejitHandlerModuleMethod(mdMethodDef methodDef, RejitHandlerModule* module);
    mdMethodDef GetMethodDef();
    RejitHandlerModule* GetModule();

    ICorProfilerFunctionControl* GetFunctionControl();
    void SetFunctionControl(ICorProfilerFunctionControl* pFunctionControl);

    FunctionInfo* GetFunctionInfo();
    void SetFunctionInfo(const FunctionInfo& functionInfo);

    MethodReplacement* GetMethodReplacement();
    void SetMethodReplacement(const MethodReplacement& methodReplacement);

    void RequestRejitForInlinersInModule(ModuleID moduleId);
};

/// <summary>
/// Rejit handler representation of a module
/// </summary>
class RejitHandlerModule
{
private:
    ModuleID m_moduleId;
    std::unique_ptr<ModuleMetadata> m_metadata;
    std::mutex m_methods_lock;
    std::unordered_map<mdMethodDef, std::unique_ptr<RejitHandlerModuleMethod>> m_methods;
    RejitHandler* m_handler;

public:
    RejitHandlerModule(ModuleID moduleId, RejitHandler* handler);
    ModuleID GetModuleId();
    RejitHandler* GetHandler();

    ModuleMetadata* GetModuleMetadata();
    void SetModuleMetadata(ModuleMetadata* metadata);

    RejitHandlerModuleMethod* GetOrAddMethod(mdMethodDef methodDef);
    bool ContainsMethod(mdMethodDef methodDef);

    void RequestRejitForInlinersInModule(ModuleID moduleId);
};

/// <summary>
/// Class to control the ReJIT mechanism and to make sure all the required
/// information is present before calling a method rewrite
/// </summary>
class RejitHandler
{
private:
    std::atomic_bool m_shutdown = {false};
    Lock m_shutdown_lock;

    std::mutex m_modules_lock;
    std::unordered_map<ModuleID, std::unique_ptr<RejitHandlerModule>> m_modules;
    AssemblyProperty* m_pCorAssemblyProperty = nullptr;

    ICorProfilerInfo4* m_profilerInfo;
    ICorProfilerInfo6* m_profilerInfo6;
    ICorProfilerInfo10* m_profilerInfo10;
    std::function<HRESULT(RejitHandlerModule*, RejitHandlerModuleMethod*)> m_rewriteCallback;

    std::unique_ptr<UniqueBlockingQueue<RejitItem>> m_rejit_queue;
    std::unique_ptr<std::thread> m_rejit_queue_thread;

    std::mutex m_ngenModules_lock;
    std::vector<ModuleID> m_ngenModules;

    static void EnqueueThreadLoop(RejitHandler* handler);

    void RequestRejitForInlinersInModule(ModuleID moduleId);
    void RequestRejit(std::vector<ModuleID>& modulesVector, std::vector<mdMethodDef>& modulesMethodDef);

public:
    RejitHandler(ICorProfilerInfo4* pInfo,
                 std::function<HRESULT(RejitHandlerModule*, RejitHandlerModuleMethod*)> rewriteCallback);
    RejitHandler(ICorProfilerInfo6* pInfo,
                 std::function<HRESULT(RejitHandlerModule*, RejitHandlerModuleMethod*)> rewriteCallback);
    RejitHandler(ICorProfilerInfo10* pInfo,
                 std::function<HRESULT(RejitHandlerModule*, RejitHandlerModuleMethod*)> rewriteCallback);

    RejitHandlerModule* GetOrAddModule(ModuleID moduleId);

    void RemoveModule(ModuleID moduleId);
    bool HasModuleAndMethod(ModuleID moduleId, mdMethodDef methodDef);

    void AddNGenModule(ModuleID moduleId);

    void EnqueueProcessModule(const std::vector<ModuleID>& modulesVector,
                              const std::vector<IntegrationMethod>& integrations,
                              std::promise<ULONG>* promise);
    void EnqueueForRejit(std::vector<ModuleID>& modulesVector, std::vector<mdMethodDef>& modulesMethodDef);

    void Shutdown();

    HRESULT NotifyReJITParameters(ModuleID moduleId, mdMethodDef methodId,
                                  ICorProfilerFunctionControl* pFunctionControl);
    HRESULT NotifyReJITCompilationStarted(FunctionID functionId, ReJITID rejitId);

    ICorProfilerInfo4* GetCorProfilerInfo();
    ICorProfilerInfo6* GetCorProfilerInfo6();

    void SetCorAssemblyProfiler(AssemblyProperty* pCorAssemblyProfiler);
    void RequestRejitForNGenInliners();
    ULONG ProcessModuleForRejit(const std::vector<ModuleID>& modules,
                                const std::vector<IntegrationMethod>& integrations,
                                bool enqueueInSameThread = false);
};

} // namespace trace

#endif // DD_CLR_PROFILER_REJIT_HANDLER_H_