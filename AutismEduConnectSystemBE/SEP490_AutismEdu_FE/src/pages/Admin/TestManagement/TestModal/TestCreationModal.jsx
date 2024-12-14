import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField } from '@mui/material'
import { enqueueSnackbar } from 'notistack';
import React, { useState } from 'react'
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';

function TestCreationModal({ dialogOpen, handleCloseDialog, setTestList, pagination, setPagination, setFilters }) {
    const [testName, setTestName] = useState('');
    const [testContent, setTestContent] = useState('');
    const [loading, setLoading] = useState(false);
    const handleCreateTest = async (e) => {
        e.preventDefault();
        try {
            setLoading(true);
            const newData = { testName: testName.trim(), testDescription: testContent.trim() };
            await services.TestManagementAPI.createTest(newData, (res) => {
                if (res?.result) {
                    enqueueSnackbar("Tạo bài kiểm tra thành công!", { variant: 'success' });
                    setPagination((prev) => ({ ...prev, pageNumber: 1 }));
                    setFilters((prev) => ({ ...prev, search: '', sort: 'desc' }));
                }
            }, (error) => {
                console.log(error);
                enqueueSnackbar(error.error[0], { variant: 'error' });
            });
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
            setTestName('');
            setTestContent('');
            handleCloseDialog();
        }

    };
    return (
        <Dialog open={dialogOpen} onClose={handleCloseDialog}>
            <form onSubmit={handleCreateTest}>
                <DialogTitle variant='h5' textAlign='center'>Tạo bài kiểm tra</DialogTitle>
                <DialogContent>
                    <Box sx={{ p: 2 }}>
                        <TextField
                            fullWidth
                            label="Tên bài kiểm tra"
                            value={testName}
                            onChange={(e) => setTestName(e.target.value)}
                            required
                            sx={{ mb: 2 }}
                        />
                        <TextField
                            fullWidth
                            label="Nội dung"
                            value={testContent}
                            onChange={(e) => setTestContent(e.target.value)}
                            multiline
                            rows={4}
                            required
                            sx={{ mb: 2 }}
                        />
                    </Box>
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleCloseDialog} variant='outlined' color="inherit">
                        Hủy
                    </Button>
                    <Button type='submit' variant='contained' color="primary">
                        Tạo
                    </Button>
                </DialogActions>
            </form>
            <LoadingComponent open={loading} setOpen={setLoading} />
        </Dialog>
    )
}

export default TestCreationModal