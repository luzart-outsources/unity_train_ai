---
tags:
- sentence-transformers
- sentence-similarity
- feature-extraction
- generated_from_trainer
- dataset_size:10058
- loss:MultipleNegativesRankingLoss
base_model: sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2
widget:
- source_sentence: sân thể dục đâu nhỉ
  sentences:
  - Đồng chí đi thẳng từ cổng chính theo hướng phía tây nam doanh trại, khu dọn vệ
    sinh ở đó.
  - sân vận động nằm ở phía đông doanh trại, từ vị trí hiện tại đi thẳng tầm 98m là
    tới.
  - khu dọn vệ sinh nằm ở phía tây nam doanh trại, từ vị trí hiện tại đi thẳng tầm
    102m là tới.
- source_sentence: Anh ơi Thanks
  sentences:
  - Không phải cảm ơn, đồng chí tập trung học tập đi.
  - Đồng ý, đồng chí đi đi. Giữ gìn kỷ luật nhé.
  - Đời lính mà đồng chí, có vất vả nhưng vui.
- source_sentence: Có mặt
  sentences:
  - Chào đồng chí, hôm nay có việc gì cần báo cáo?
  - Đồng chí đi thẳng từ cổng chính theo hướng trung tâm doanh trại, nhà ăn ở đó.
  - Muốn tới ký túc xá thì đi phía nam doanh trại doanh trại, có biển chỉ dẫn rồi
    đó.
- source_sentence: căng tin ở đâu
  sentences:
  - nhà ăn nằm ở trung tâm doanh trại, từ vị trí hiện tại đi thẳng tầm 65m là tới.
  - khu tự do ở phía đông nam doanh trại, đi theo đường nội bộ tầm 61m thấy ngay.
  - nhà ăn nằm ở trung tâm doanh trại, từ vị trí hiện tại đi thẳng tầm 65m là tới.
- source_sentence: Anh ơi Cho hỏi ky tuc xa ở đâu
  sentences:
  - ký túc xá ở phía nam doanh trại, đi theo đường nội bộ tầm 110m thấy ngay.
  - ký túc xá nằm ở phía nam doanh trại, từ vị trí hiện tại đi thẳng tầm 110m là tới.
  - Học GDQuocPhong bắt đầu lúc 14:00 và kết thúc lúc 17:00.
pipeline_tag: sentence-similarity
library_name: sentence-transformers
metrics:
- cosine_accuracy@1
- cosine_accuracy@3
- cosine_accuracy@5
- cosine_accuracy@10
- cosine_precision@1
- cosine_precision@5
- cosine_recall@1
- cosine_recall@5
- cosine_ndcg@1
- cosine_ndcg@5
- cosine_ndcg@10
- cosine_mrr@1
- cosine_mrr@5
- cosine_mrr@10
- cosine_map@100
model-index:
- name: SentenceTransformer based on sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2
  results:
  - task:
      type: information-retrieval
      name: Information Retrieval
    dataset:
      name: academy qa ft
      type: academy_qa_ft
    metrics:
    - type: cosine_accuracy@1
      value: 0.047229791099000905
      name: Cosine Accuracy@1
    - type: cosine_accuracy@3
      value: 0.10717529518619437
      name: Cosine Accuracy@3
    - type: cosine_accuracy@5
      value: 0.15440508628519528
      name: Cosine Accuracy@5
    - type: cosine_accuracy@10
      value: 0.27702089009990916
      name: Cosine Accuracy@10
    - type: cosine_precision@1
      value: 0.047229791099000905
      name: Cosine Precision@1
    - type: cosine_precision@5
      value: 0.030881017257039057
      name: Cosine Precision@5
    - type: cosine_recall@1
      value: 0.047229791099000905
      name: Cosine Recall@1
    - type: cosine_recall@5
      value: 0.15440508628519528
      name: Cosine Recall@5
    - type: cosine_ndcg@1
      value: 0.047229791099000905
      name: Cosine Ndcg@1
    - type: cosine_ndcg@5
      value: 0.10047254234032994
      name: Cosine Ndcg@5
    - type: cosine_ndcg@10
      value: 0.14025432977310737
      name: Cosine Ndcg@10
    - type: cosine_mrr@1
      value: 0.047229791099000905
      name: Cosine Mrr@1
    - type: cosine_mrr@5
      value: 0.08287920072661217
      name: Cosine Mrr@5
    - type: cosine_mrr@10
      value: 0.0993652956187016
      name: Cosine Mrr@10
    - type: cosine_map@100
      value: 0.11672850203233856
      name: Cosine Map@100
---

# SentenceTransformer based on sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2

This is a [sentence-transformers](https://www.SBERT.net) model finetuned from [sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2](https://huggingface.co/sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2). It maps sentences & paragraphs to a 384-dimensional dense vector space and can be used for retrieval.

## Model Details

### Model Description
- **Model Type:** Sentence Transformer
- **Base model:** [sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2](https://huggingface.co/sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2) <!-- at revision e8f8c211226b894fcb81acc59f3b34ba3efd5f42 -->
- **Maximum Sequence Length:** 128 tokens
- **Output Dimensionality:** 384 dimensions
- **Similarity Function:** Cosine Similarity
- **Supported Modality:** Text
<!-- - **Training Dataset:** Unknown -->
<!-- - **Language:** Unknown -->
<!-- - **License:** Unknown -->

### Model Sources

- **Documentation:** [Sentence Transformers Documentation](https://sbert.net)
- **Repository:** [Sentence Transformers on GitHub](https://github.com/huggingface/sentence-transformers)
- **Hugging Face:** [Sentence Transformers on Hugging Face](https://huggingface.co/models?library=sentence-transformers)

### Full Model Architecture

```
SentenceTransformer(
  (0): Transformer({'transformer_task': 'feature-extraction', 'modality_config': {'text': {'method': 'forward', 'method_output_name': 'last_hidden_state'}}, 'module_output_name': 'token_embeddings', 'architecture': 'BertModel'})
  (1): Pooling({'embedding_dimension': 384, 'pooling_mode': 'mean', 'include_prompt': True})
)
```

## Usage

### Direct Usage (Sentence Transformers)

First install the Sentence Transformers library:

```bash
pip install -U sentence-transformers
```
Then you can load this model and run inference.
```python
from sentence_transformers import SentenceTransformer

# Download from the 🤗 Hub
model = SentenceTransformer("sentence_transformers_model_id")
# Run inference
queries = [
    'Anh ơi Cho hỏi ky tuc xa ở đâu',
]
documents = [
    'ký túc xá ở phía nam doanh trại, đi theo đường nội bộ tầm 110m thấy ngay.',
    'ký túc xá nằm ở phía nam doanh trại, từ vị trí hiện tại đi thẳng tầm 110m là tới.',
    'Học GDQuocPhong bắt đầu lúc 14:00 và kết thúc lúc 17:00.',
]
query_embeddings = model.encode_query(queries)
document_embeddings = model.encode_document(documents)
print(query_embeddings.shape, document_embeddings.shape)
# [1, 384] [3, 384]

# Get the similarity scores for the embeddings
similarities = model.similarity(query_embeddings, document_embeddings)
print(similarities)
# tensor([[ 0.7007,  0.6873, -0.2420]])
```
<!--
### Direct Usage (Transformers)

<details><summary>Click to see the direct usage in Transformers</summary>

</details>
-->

<!--
### Downstream Usage (Sentence Transformers)

You can finetune this model on your own dataset.

<details><summary>Click to expand</summary>

</details>
-->

<!--
### Out-of-Scope Use

*List how the model may foreseeably be misused and address what users ought not to do with the model.*
-->

## Evaluation

### Metrics

#### Information Retrieval

* Dataset: `academy_qa_ft`
* Evaluated with [<code>InformationRetrievalEvaluator</code>](https://sbert.net/docs/package_reference/sentence_transformer/evaluation.html#sentence_transformers.sentence_transformer.evaluation.InformationRetrievalEvaluator)

| Metric             | Value      |
|:-------------------|:-----------|
| cosine_accuracy@1  | 0.0472     |
| cosine_accuracy@3  | 0.1072     |
| cosine_accuracy@5  | 0.1544     |
| cosine_accuracy@10 | 0.277      |
| cosine_precision@1 | 0.0472     |
| cosine_precision@5 | 0.0309     |
| cosine_recall@1    | 0.0472     |
| cosine_recall@5    | 0.1544     |
| cosine_ndcg@1      | 0.0472     |
| cosine_ndcg@5      | 0.1005     |
| **cosine_ndcg@10** | **0.1403** |
| cosine_mrr@1       | 0.0472     |
| cosine_mrr@5       | 0.0829     |
| cosine_mrr@10      | 0.0994     |
| cosine_map@100     | 0.1167     |

<!--
## Bias, Risks and Limitations

*What are the known or foreseeable issues stemming from this model? You could also flag here known failure cases or weaknesses of the model.*
-->

<!--
### Recommendations

*What are recommendations with respect to the foreseeable issues? For example, filtering explicit content.*
-->

## Training Details

### Training Dataset

#### Unnamed Dataset

* Size: 10,058 training samples
* Columns: <code>sentence_0</code> and <code>sentence_1</code>
* Approximate statistics based on the first 100 samples:
  |          | sentence_0                                                                        | sentence_1                                                                         |
  |:---------|:----------------------------------------------------------------------------------|:-----------------------------------------------------------------------------------|
  | type     | string                                                                            | string                                                                             |
  | modality | text                                                                              | text                                                                               |
  | details  | <ul><li>min: 2 tokens</li><li>mean: 10.02 tokens</li><li>max: 16 tokens</li></ul> | <ul><li>min: 11 tokens</li><li>mean: 21.88 tokens</li><li>max: 27 tokens</li></ul> |
* Samples:
  | sentence_0                          | sentence_1                                                                                               |
  |:------------------------------------|:---------------------------------------------------------------------------------------------------------|
  | <code>Cho hoi lop hoc o dau</code>  | <code>lớp học là khu nằm phía phía bắc doanh trại doanh trại, dễ tìm thôi đồng chí.</code>               |
  | <code>khu dọn dẹp đi lối nào</code> | <code>khu dọn vệ sinh nằm ở phía tây nam doanh trại, từ vị trí hiện tại đi thẳng tầm 102m là tới.</code> |
  | <code>Mai co lich gi</code>         | <code>{__SCHEDULE_TODAY__}</code>                                                                        |
* Loss: [<code>MultipleNegativesRankingLoss</code>](https://sbert.net/docs/package_reference/sentence_transformer/losses.html#multiplenegativesrankingloss) with these parameters:
  ```json
  {
      "scale": 20.0,
      "similarity_fct": "cos_sim",
      "gather_across_devices": false,
      "directions": [
          "query_to_doc"
      ],
      "partition_mode": "joint",
      "hardness_mode": null,
      "hardness_strength": 0.0
  }
  ```

### Training Hyperparameters
#### Non-Default Hyperparameters

- `per_device_train_batch_size`: 32
- `num_train_epochs`: 6
- `per_device_eval_batch_size`: 32
- `multi_dataset_batch_sampler`: round_robin

#### All Hyperparameters
<details><summary>Click to expand</summary>

- `per_device_train_batch_size`: 32
- `num_train_epochs`: 6
- `max_steps`: -1
- `learning_rate`: 5e-05
- `lr_scheduler_type`: linear
- `lr_scheduler_kwargs`: None
- `warmup_steps`: 0
- `optim`: adamw_torch
- `optim_args`: None
- `weight_decay`: 0.0
- `adam_beta1`: 0.9
- `adam_beta2`: 0.999
- `adam_epsilon`: 1e-08
- `optim_target_modules`: None
- `gradient_accumulation_steps`: 1
- `average_tokens_across_devices`: True
- `max_grad_norm`: 1
- `label_smoothing_factor`: 0.0
- `bf16`: False
- `fp16`: False
- `bf16_full_eval`: False
- `fp16_full_eval`: False
- `tf32`: None
- `gradient_checkpointing`: False
- `gradient_checkpointing_kwargs`: None
- `torch_compile`: False
- `torch_compile_backend`: None
- `torch_compile_mode`: None
- `use_liger_kernel`: False
- `liger_kernel_config`: None
- `use_cache`: False
- `neftune_noise_alpha`: None
- `torch_empty_cache_steps`: None
- `auto_find_batch_size`: False
- `log_on_each_node`: True
- `logging_nan_inf_filter`: True
- `include_num_input_tokens_seen`: no
- `log_level`: passive
- `log_level_replica`: warning
- `disable_tqdm`: False
- `project`: huggingface
- `trackio_space_id`: None
- `trackio_bucket_id`: None
- `trackio_static_space_id`: None
- `per_device_eval_batch_size`: 32
- `prediction_loss_only`: True
- `eval_on_start`: False
- `eval_do_concat_batches`: True
- `eval_use_gather_object`: False
- `eval_accumulation_steps`: None
- `include_for_metrics`: []
- `batch_eval_metrics`: False
- `save_only_model`: False
- `save_on_each_node`: False
- `enable_jit_checkpoint`: False
- `push_to_hub`: False
- `hub_private_repo`: None
- `hub_model_id`: None
- `hub_strategy`: every_save
- `hub_always_push`: False
- `hub_revision`: None
- `load_best_model_at_end`: False
- `ignore_data_skip`: False
- `restore_callback_states_from_checkpoint`: False
- `full_determinism`: False
- `seed`: 42
- `data_seed`: None
- `use_cpu`: False
- `accelerator_config`: {'split_batches': False, 'dispatch_batches': None, 'even_batches': True, 'use_seedable_sampler': True, 'non_blocking': False, 'gradient_accumulation_kwargs': None}
- `parallelism_config`: None
- `dataloader_drop_last`: False
- `dataloader_num_workers`: 0
- `dataloader_pin_memory`: True
- `dataloader_persistent_workers`: False
- `dataloader_prefetch_factor`: None
- `remove_unused_columns`: True
- `label_names`: None
- `train_sampling_strategy`: random
- `length_column_name`: length
- `ddp_find_unused_parameters`: None
- `ddp_bucket_cap_mb`: None
- `ddp_broadcast_buffers`: False
- `ddp_static_graph`: None
- `ddp_backend`: None
- `ddp_timeout`: 1800
- `fsdp`: []
- `fsdp_config`: {'min_num_params': 0, 'xla': False, 'xla_fsdp_v2': False, 'xla_fsdp_grad_ckpt': False}
- `deepspeed`: None
- `debug`: []
- `skip_memory_metrics`: True
- `do_predict`: False
- `resume_from_checkpoint`: None
- `warmup_ratio`: None
- `local_rank`: -1
- `prompts`: None
- `batch_sampler`: batch_sampler
- `multi_dataset_batch_sampler`: round_robin
- `router_mapping`: {}
- `learning_rate_mapping`: {}

</details>

### Training Logs
| Epoch  | Step | Training Loss | academy_qa_ft_cosine_ndcg@10 |
|:------:|:----:|:-------------:|:----------------------------:|
| -1     | -1   | -             | 0.0524                       |
| 0.4984 | 157  | -             | 0.0970                       |
| 0.9968 | 314  | -             | 0.1096                       |
| 1.0    | 315  | -             | 0.1045                       |
| 1.4952 | 471  | -             | 0.1146                       |
| 1.5873 | 500  | 1.6292        | -                            |
| 1.9937 | 628  | -             | 0.1165                       |
| 2.0    | 630  | -             | 0.1157                       |
| 2.4921 | 785  | -             | 0.1274                       |
| 2.9905 | 942  | -             | 0.1249                       |
| 3.0    | 945  | -             | 0.1241                       |
| 3.1746 | 1000 | 1.1841        | -                            |
| 3.4889 | 1099 | -             | 0.1252                       |
| 3.9873 | 1256 | -             | 0.1280                       |
| 4.0    | 1260 | -             | 0.1270                       |
| 4.4857 | 1413 | -             | 0.1302                       |
| 4.7619 | 1500 | 1.1565        | -                            |
| 4.9841 | 1570 | -             | 0.1371                       |
| 5.0    | 1575 | -             | 0.1403                       |


### Training Time
- **Training**: 28.0 minutes
- **Evaluation**: 52.9 seconds
- **Total**: 28.9 minutes

### Framework Versions
- Python: 3.10.11
- Sentence Transformers: 5.5.1
- Transformers: 5.9.0
- PyTorch: 2.5.1+cpu
- Accelerate: 1.13.0
- Datasets: 4.8.5
- Tokenizers: 0.22.2

## Citation

### BibTeX

#### Sentence Transformers
```bibtex
@inproceedings{reimers-2019-sentence-bert,
    title = "Sentence-BERT: Sentence Embeddings using Siamese BERT-Networks",
    author = "Reimers, Nils and Gurevych, Iryna",
    booktitle = "Proceedings of the 2019 Conference on Empirical Methods in Natural Language Processing",
    month = "11",
    year = "2019",
    publisher = "Association for Computational Linguistics",
    url = "https://arxiv.org/abs/1908.10084",
}
```

#### MultipleNegativesRankingLoss
```bibtex
@misc{oord2019representationlearningcontrastivepredictive,
      title={Representation Learning with Contrastive Predictive Coding},
      author={Aaron van den Oord and Yazhe Li and Oriol Vinyals},
      year={2019},
      eprint={1807.03748},
      archivePrefix={arXiv},
      primaryClass={cs.LG},
      url={https://arxiv.org/abs/1807.03748},
}
```

<!--
## Glossary

*Clearly define terms in order to be accessible across audiences.*
-->

<!--
## Model Card Authors

*Lists the people who create the model card, providing recognition and accountability for the detailed work that goes into its construction.*
-->

<!--
## Model Card Contact

*Provides a way for people who have updates to the Model Card, suggestions, or questions, to contact the Model Card authors.*
-->