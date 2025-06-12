/**
 * Media Preview System for Post Creation
 * Handles image and video file selection, preview, and validation
 */

class MediaPreviewManager {
    constructor(options = {}) {
        // Configuration
        this.config = {
            maxImageSize: options.maxImageSize || 10 * 1024 * 1024, // 10MB
            maxVideoSize: options.maxVideoSize || 100 * 1024 * 1024, // 100MB
            maxTotalFiles: options.maxTotalFiles || 4,
            imageInputId: options.imageInputId || 'imagesInput',
            videoInputId: options.videoInputId || 'videosInput',
            previewContainerId: options.previewContainerId || 'mediaPreviewContainer',
            previewAreaId: options.previewAreaId || 'mediaPreview',
            emptyPreviewId: options.emptyPreviewId || 'emptyPreview',
            imageCountId: options.imageCountId || 'imageCount',
            videoCountId: options.videoCountId || 'videoCount',
            formId: options.formId || 'createPostForm',
            submitButtonId: options.submitButtonId || 'shareButton'
        };

        // State
        this.selectedImages = [];
        this.selectedVideos = [];

        // Initialize
        this.init();
    }

    init() {
        this.bindEvents();
        this.initializeTooltips();
    }

    bindEvents() {
        // File input events
        $(`#${this.config.imageInputId}`).on('change', (e) => this.handleImageInput(e));
        $(`#${this.config.videoInputId}`).on('change', (e) => this.handleVideoInput(e));

        // Remove media events
        $(document).on('click', '.media-remove-btn', (e) => this.handleRemoveMedia(e));

        // Form submit event
        $(`#${this.config.formId}`).on('submit', (e) => this.handleFormSubmit(e));

        // Cleanup on page unload
        $(window).on('beforeunload', () => this.cleanup());
    }

    initializeTooltips() {
        if (typeof bootstrap !== 'undefined') {
            $('[data-bs-toggle="tooltip"]').tooltip();
        }
    }

    /**
     * Show toast notification
     */
    showToast(message, type = 'danger') {
        const toastId = 'toast-' + Date.now();
        const icon = type === 'danger' ? 'bx-error' : 'bx-check';
        const title = type === 'danger' ? 'Error' : 'Success';

        const toastHtml = `
            <div class="toast" id="${toastId}" role="alert" aria-live="assertive" aria-atomic="true" 
                 data-bs-autohide="true" data-bs-delay="5000">
                <div class="toast-header bg-${type} text-white">
                    <i class="bx ${icon} me-2"></i>
                    <strong class="me-auto">${title}</strong>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
                </div>
                <div class="toast-body">
                    ${message}
                </div>
            </div>
        `;

        if ($('.toast-container').length === 0) {
            $('body').append('<div class="toast-container position-fixed bottom-0 end-0 p-3"></div>');
        }

        $('.toast-container').append(toastHtml);

        if (typeof bootstrap !== 'undefined') {
            const toast = new bootstrap.Toast(document.getElementById(toastId));
            toast.show();

            // Remove toast element after it's hidden
            document.getElementById(toastId).addEventListener('hidden.bs.toast', function() {
                this.remove();
            });
        } else {
            // Fallback for non-Bootstrap environments
            setTimeout(() => {
                $(`#${toastId}`).fadeOut(400, function() {
                    $(this).remove();
                });
            }, 5000);
        }
    }

    /**
     * Validate file size
     */
    validateFileSize(file, isVideo = false) {
        const maxSize = isVideo ? this.config.maxVideoSize : this.config.maxImageSize;
        const fileType = isVideo ? 'video' : 'image';

        if (file.size > maxSize) {
            const maxSizeMB = Math.round(maxSize / (1024 * 1024));
            this.showToast(`File "${file.name}" exceeds the maximum ${fileType} size of ${maxSizeMB}MB.`);
            return false;
        }
        return true;
    }

    /**
     * Update file count badges
     */
    updateFileCounts() {
        const totalImages = this.selectedImages.length;
        const totalVideos = this.selectedVideos.length;

        const imageCountEl = $(`#${this.config.imageCountId}`);
        const videoCountEl = $(`#${this.config.videoCountId}`);

        if (totalImages > 0) {
            imageCountEl.text(totalImages).removeClass('d-none');
        } else {
            imageCountEl.addClass('d-none');
        }

        if (totalVideos > 0) {
            videoCountEl.text(totalVideos).removeClass('d-none');
        } else {
            videoCountEl.addClass('d-none');
        }
    }

    /**
     * Update media preview display
     */
    updateMediaPreview() {
        const $container = $(`#${this.config.previewContainerId}`);
        const $preview = $(`#${this.config.previewAreaId}`);
        const $emptyPreview = $(`#${this.config.emptyPreviewId}`);

        // Clear existing preview
        $preview.empty();

        const allFiles = [...this.selectedImages, ...this.selectedVideos];

        if (allFiles.length === 0) {
            $container.removeClass('has-files');
            $emptyPreview.show();
            return;
        }

        $container.addClass('has-files');
        $emptyPreview.hide();

        // Create preview for each file
        allFiles.forEach((fileData, index) => {
            const colClass = this.getColumnClass(allFiles.length);

            const mediaElement = fileData.type === 'image' ?
                `<img src="${fileData.url}" alt="${fileData.name}" />` :
                `<video src="${fileData.url}" controls preload="metadata"></video>`;

            const $mediaItem = $(`
                <div class="${colClass}">
                    <div class="media-preview-item" data-file-id="${fileData.id}">
                        ${mediaElement}
                        <button type="button" class="media-remove-btn" 
                                data-file-id="${fileData.id}" data-file-type="${fileData.type}"
                                title="Remove file">
                            <i class="bx bx-x"></i>
                        </button>
                    </div>
                </div>
            `);

            $preview.append($mediaItem);
        });
    }

    /**
     * Get appropriate column class based on number of files
     */
    getColumnClass(fileCount) {
        if (fileCount === 1) return 'col-12';
        if (fileCount === 2) return 'col-6';
        if (fileCount === 3) return 'col-4';
        return 'col-6 col-md-3';
    }

    /**
     * Process selected files
     */
    processFiles(files, type) {
        const fileArray = Array.from(files);
        const newFiles = [];

        // Validate each file
        for (let file of fileArray) {
            if (!this.validateFileSize(file, type === 'video')) {
                return false; // Stop processing if any file is invalid
            }

            // Check total file limit
            const currentTotal = this.selectedImages.length + this.selectedVideos.length;
            if (currentTotal + newFiles.length >= this.config.maxTotalFiles) {
                this.showToast(`You can only upload up to ${this.config.maxTotalFiles} files total.`);
                return false;
            }

            // Create file data object
            const fileData = {
                id: Date.now() + Math.random(),
                file: file,
                name: file.name,
                type: type,
                url: URL.createObjectURL(file)
            };

            newFiles.push(fileData);
        }

        // Add to appropriate array
        if (type === 'image') {
            this.selectedImages.push(...newFiles);
        } else {
            this.selectedVideos.push(...newFiles);
        }

        return true;
    }

    /**
     * Update form inputs with selected files
     */
    updateFormInputs() {
        // Update image input
        const imageDataTransfer = new DataTransfer();
        this.selectedImages.forEach(fileData => {
            imageDataTransfer.items.add(fileData.file);
        });
        document.getElementById(this.config.imageInputId).files = imageDataTransfer.files;

        // Update video input
        const videoDataTransfer = new DataTransfer();
        this.selectedVideos.forEach(fileData => {
            videoDataTransfer.items.add(fileData.file);
        });
        document.getElementById(this.config.videoInputId).files = videoDataTransfer.files;
    }

    /**
     * Handle image input change
     */
    handleImageInput(e) {
        const files = e.target.files;
        if (files.length > 0) {
            if (this.processFiles(files, 'image')) {
                this.updateMediaPreview();
                this.updateFileCounts();
                this.updateFormInputs();
                this.showToast(`Successfully added ${files.length} image(s).`, 'success');
            } else {
                // Reset input if validation failed
                e.target.value = '';
            }
        }
    }

    /**
     * Handle video input change
     */
    handleVideoInput(e) {
        const files = e.target.files;
        if (files.length > 0) {
            if (this.processFiles(files, 'video')) {
                this.updateMediaPreview();
                this.updateFileCounts();
                this.updateFormInputs();
                this.showToast(`Successfully added ${files.length} video(s).`, 'success');
            } else {
                // Reset input if validation failed
                e.target.value = '';
            }
        }
    }

    /**
     * Handle remove media item
     */
    handleRemoveMedia(e) {
        e.preventDefault();
        e.stopPropagation();

        const fileId = $(e.target).closest('.media-remove-btn').data('file-id');
        const fileType = $(e.target).closest('.media-remove-btn').data('file-type');

        // Remove from appropriate array
        if (fileType === 'image') {
            const index = this.selectedImages.findIndex(f => f.id === fileId);
            if (index > -1) {
                URL.revokeObjectURL(this.selectedImages[index].url);
                this.selectedImages.splice(index, 1);
            }
        } else {
            const index = this.selectedVideos.findIndex(f => f.id === fileId);
            if (index > -1) {
                URL.revokeObjectURL(this.selectedVideos[index].url);
                this.selectedVideos.splice(index, 1);
            }
        }

        // Update UI
        this.updateMediaPreview();
        this.updateFileCounts();
        this.updateFormInputs();

        this.showToast('File removed successfully.', 'success');
    }

    /**
     * Handle form submit
     */
    handleFormSubmit(e) {
        // You can add custom validation here
        const title = $(`#${this.config.formId} input[name="Title"]`).val()?.trim();

        if (!title) {
            e.preventDefault();
            this.showToast('Please enter a title for your post.');
            return false;
        }

        // Add loading state to submit button
        const $submitBtn = $(`#${this.config.submitButtonId}`);
        const originalText = $submitBtn.html();

        $submitBtn.prop('disabled', true)
            .addClass('btn-loading')
            .html('<span class="spinner-border spinner-border-sm me-2"></span>Posting...');

        // Note: The actual form submission will be handled by your existing AJAX code
        // This is just for the loading state. Make sure to restore the button state
        // in your success/error callbacks:

        // setTimeout(() => {
        //     $submitBtn.prop('disabled', false)
        //              .removeClass('btn-loading')
        //              .html(originalText);
        // }, 2000);
    }

    /**
     * Reset the media preview
     */
    reset() {
        // Clean up object URLs
        this.cleanup();

        // Reset arrays
        this.selectedImages = [];
        this.selectedVideos = [];

        // Update UI
        this.updateMediaPreview();
        this.updateFileCounts();
        this.updateFormInputs();
    }

    /**
     * Clean up object URLs to prevent memory leaks
     */
    cleanup() {
        [...this.selectedImages, ...this.selectedVideos].forEach(fileData => {
            URL.revokeObjectURL(fileData.url);
        });
    }

    /**
     * Get current file counts
     */
    getFileCounts() {
        return {
            images: this.selectedImages.length,
            videos: this.selectedVideos.length,
            total: this.selectedImages.length + this.selectedVideos.length
        };
    }
}

// Initialize when document is ready
$(document).ready(function() {
    // Create global instance
    window.mediaPreviewManager = new MediaPreviewManager();

    // Example of how to customize configuration:
    // window.mediaPreviewManager = new MediaPreviewManager({
    //     maxImageSize: 15 * 1024 * 1024, // 15MB
    //     maxVideoSize: 200 * 1024 * 1024, // 200MB
    //     maxTotalFiles: 6
    // });
});