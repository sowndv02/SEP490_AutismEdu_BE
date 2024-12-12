import { useEffect, useState } from 'react';
import { Box, Typography, Stack, TableContainer, Table, TableHead, TableRow, TableCell, TableBody, Paper, Button, Pagination, FormControl, InputLabel, Select, MenuItem, Dialog, DialogTitle, DialogContent, DialogActions, Divider, Modal } from '@mui/material';
import services from '~/plugins/services';
import LoadingComponent from '~/components/LoadingComponent';
import emptyBook from '~/assets/images/icon/emptybook.gif'

const TutorRequestHistory = () => {

  const [selectedContent, setSelectedContent] = useState('');
  const [modalDescription, setModalDescription] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [openDialog, setOpenDialog] = useState(false);
  const [requestList, setRequestList] = useState([]);
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState({
    status: 'all',
    orderBy: 'createdDate',
    sort: 'desc',
  });

  const [pagination, setPagination] = useState({
    pageNumber: 1,
    pageSize: 5,
    total: 5,
  });

  useEffect(() => {
    window.scrollTo(0, 0);
    handleGetRequestList();
  }, [filters, pagination.pageNumber]);

  const handleGetRequestList = async () => {
    try {
      setLoading(true);
      await services.TutorRequestAPI.getListRequestHistory((res) => {
        if (res?.result) {
          setRequestList(res?.result);
          setPagination(res?.pagination);
        }
      }, (error) => {
        console.log(error);
      }, {
        status: filters.status,
        orderBy: filters.orderBy,
        sort: filters.sort,
        pageNumber: pagination?.pageNumber
      });
    } catch (error) {
      console.log(error);
    } finally {
      setLoading(false);
    }
  };

  const handleCloseModal = () => {
    setModalOpen(false);
  };

  const handleOpenModal = (description) => {
    setModalDescription(description);
    setModalOpen(true);
  };

  const handleCloseDialog = () => {
    setOpenDialog(false);
    setSelectedContent('');
  };

  const handleOpenDialog = (content) => {
    setSelectedContent(content);
    setOpenDialog(true);
  };

  const handleFilterChange = (field) => (event) => {
    setFilters({ ...filters, [field]: event.target.value });
    setPagination({
      ...pagination,
      pageNumber: 1,
    });
  };

  const handlePageChange = (event, value) => {
    setPagination({ ...pagination, pageNumber: value });
  };

  const statusText = (status) => {
    return status === 1 ? 'Đã chấp nhận' : status === 0 ? 'Từ chối' : 'Đang chờ';
  };

  const statusTypeReject = (status) => {
    return status === 3 ? 'Lý do khác' : status === 2 ? 'Xung đột lịch trình' : status === 1 ? 'Không tương thích với chương trình giảng dạy' : '-';
  };

  const totalPages = Math.ceil(pagination.total / pagination.pageSize);

  return (
    <Box sx={{ p: 5, width: "90%", mx: "auto", gap: 2, height: requestList.length < 5 ? '700px' : "auto" }}>
      <Typography variant='h4' sx={{ mb: 3 }} textAlign={'center'}>Lịch sử yêu cầu đã gửi</Typography>
      <Stack direction={'row'} justifyContent={'space-between'} alignItems="center" sx={{ width: "100%", mb: 2 }} spacing={3}>
        <Stack direction={'row'} justifyContent={'flex-end'} spacing={2} sx={{ flex: 1 }}>
          <Box sx={{ flexBasis: '10%' }}>
            <FormControl fullWidth size='small'>
              <InputLabel id="status-select-label">Trạng thái</InputLabel>
              <Select
                labelId="status-select-label"
                value={filters.status}
                label="Trạng thái"
                onChange={handleFilterChange('status')}
                sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
              >
                <MenuItem value={'all'}>Tất cả</MenuItem>
                <MenuItem value={'approve'}>Đã chấp nhận</MenuItem>
                <MenuItem value={'pending'}>Đang chờ</MenuItem>
                <MenuItem value={'reject'}>Từ chối</MenuItem>
              </Select>
            </FormControl>
          </Box>

          <Box sx={{ flexBasis: '10%' }}>
            <FormControl fullWidth size='small'>
              <InputLabel id="sort-select-label">Thứ tự</InputLabel>
              <Select
                labelId="sort-select-label"
                value={filters.sort}
                label="Thứ tự"
                onChange={handleFilterChange('sort')}
                sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
              >
                <MenuItem value="asc">Tăng dần theo ngày</MenuItem>
                <MenuItem value="desc">Giảm dần theo ngày</MenuItem>
              </Select>
            </FormControl>
          </Box>
        </Stack>
      </Stack>

      <Box>
        {requestList.length !== 0 ? <>
          <TableContainer component={Paper} sx={{ mt: 3, boxShadow: 3, borderRadius: 2 }}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 'bold' }}>STT</TableCell>
                  <TableCell sx={{ fontWeight: 'bold' }}>Tên gia sư</TableCell>
                  <TableCell sx={{ fontWeight: 'bold' }}>Tên trẻ</TableCell>
                  <TableCell sx={{ fontWeight: 'bold' }}>Nội dung</TableCell>
                  <TableCell sx={{ fontWeight: 'bold' }}>Ngày tạo</TableCell>
                  <TableCell sx={{ fontWeight: 'bold' }}>Loại từ chối</TableCell>
                  <TableCell sx={{ fontWeight: 'bold' }}>Lý do từ chối</TableCell>
                  <TableCell sx={{ fontWeight: 'bold' }}>Trạng thái</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {requestList.map((request, index) => (
                  <TableRow key={request.id} hover>
                    <TableCell>{index + 1 + (pagination?.pageNumber - 1) * 5}</TableCell>
                    <TableCell>{request.tutor?.fullName}</TableCell>
                    <TableCell>{request?.childInformation?.name}</TableCell>
                    <TableCell sx={{ maxWidth: 290 }}>
                      {request?.description ?
                        (
                          <Box sx={{ display: 'inline-flex', alignItems: 'center' }}>
                            <Box sx={{
                              overflow: 'hidden',
                              textOverflow: 'ellipsis',
                              whiteSpace: 'nowrap',
                              maxWidth: 250
                            }}>
                              {request?.description}
                            </Box>
                            {request?.description.length > 35 && (
                              <Button
                                variant="text"
                                size="small"
                                onClick={() => handleOpenModal(request?.description)}
                                sx={{ textTransform: 'none', color: 'primary.main' }}
                              >
                                Xem thêm
                              </Button>
                            )}
                          </Box>
                        )
                        : '-'}

                    </TableCell>
                    <TableCell>{request.createdDate && new Date(request.createdDate)?.toLocaleDateString()}</TableCell>
                    <TableCell sx={{ maxWidth: 200 }}>{statusTypeReject(request?.rejectType) || 'N/A'}</TableCell>
                    <TableCell>

                      {request?.rejectionReason ?
                        (
                          <Box sx={{ display: 'inline-flex', alignItems: 'center' }}>
                            <Box sx={{
                              overflow: 'hidden',
                              textOverflow: 'ellipsis',
                              whiteSpace: 'nowrap',
                              maxWidth: 250
                            }}>
                              {request?.rejectionReason}
                            </Box>
                            {request?.rejectionReason.length > 35 && (
                              <Button
                                variant="text"
                                size="small"
                                onClick={() => handleOpenDialog(request?.rejectionReason)}
                                sx={{ textTransform: 'none', color: 'primary.main' }}
                              >
                                Xem thêm
                              </Button>
                            )}
                          </Box>
                        )
                        : '-'}
                    </TableCell>
                    <TableCell>
                      <Button
                        variant="outlined"
                        color={
                          request.requestStatus === 1 ? 'success' :
                            request.requestStatus === 0 ? 'error' :
                              'warning'
                        }
                        size="small"
                        sx={{ borderRadius: 2, textTransform: 'none' }}
                      >
                        {statusText(request.requestStatus)}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <Stack direction="row" justifyContent="center" sx={{ mt: 3 }}>
            <Pagination
              count={totalPages}
              page={pagination.pageNumber}
              onChange={handlePageChange}
              color="primary"
            />
          </Stack></> : <Box sx={{ textAlign: "center" }}>
                            <img src={emptyBook} style={{ height: "200px" }} />
                            <Typography>Hiện tại chưa có lịch sử yêu cầu nào được gửi!</Typography>
                        </Box>}
      </Box>
      <Modal
        open={openDialog}
        onClose={handleCloseDialog}
        aria-labelledby="modal-reason-title"
        aria-describedby="modal-reason-description"
      >
        <Box
          sx={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%, -50%)'
            , maxWidth: 600
            , width: '100%',
            boxShadow: 24,
            p: 3,
            borderRadius: '10px',
            backgroundColor: 'white'
          }}
        >
          <Typography
            variant="h5"
            textAlign="center"
            gutterBottom
          >
            Lý do từ chối
          </Typography>
          <Divider sx={{ mb: 2 }} />
          <Typography variant="body1">{selectedContent}</Typography>
          <Box
            sx={{
              display: 'flex',
              justifyContent: 'flex-end',
              mt: 3,
            }}
          >
            <Button onClick={handleCloseDialog} variant="outlined" color="primary">
              Đóng
            </Button>
          </Box>
        </Box>
      </Modal>
      <Modal
        open={modalOpen}
        onClose={handleCloseModal}
        sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center' }}
      >
        <Box sx={{ backgroundColor: 'white', padding: 4, maxWidth: 600, width: '100%', borderRadius: '10px' }}>
          <Typography variant="h5" mb={2} textAlign="center">Nội dung</Typography>
          <Divider sx={{ mb: 2 }} />
          <Typography variant="body1">{modalDescription}</Typography>
          <Box mt={2} display={'flex'} justifyContent={'flex-end'}>
            <Button variant="contained" onClick={handleCloseModal}>Đóng</Button>
          </Box>
        </Box>
      </Modal>
      <LoadingComponent open={loading} setOpen={setLoading} />
    </Box>
  );
};

export default TutorRequestHistory;
